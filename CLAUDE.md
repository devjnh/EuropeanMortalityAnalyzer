# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

C# console app that downloads EuroStat weekly deaths, EuroStat population age structure and ECDC Covid vaccination data, loads them into a local SQLite database, and generates Excel spreadsheets (EPPlus) with mortality evolution, projections from a regression baseline, excess mortality, and Z-scores. Root namespace is `MortalityAnalyzer`.

## Build and run

Single project `EuropeanMortalityAnalyzer/EuropeanMortalityAnalyzer.csproj`, multi-targeted `net472` (the primary Windows build) and `net6.0` (untested on Linux/macOS). C# 10, nullable enabled, implicit usings disabled (add `using` directives explicitly).

```bash
dotnet build EuropeanMortalityAnalyzer.sln
dotnet run --project EuropeanMortalityAnalyzer -f net6.0 -- countries --Countries FR ES IT --MinAge 5 --MaxAge 40
dotnet run --project EuropeanMortalityAnalyzer -f net6.0 -- help
```

There are no tests and no lint configuration.

CLI verbs (CommandLineParser, defined in `Options.cs`): `init`, `show`, `area`, `countries`, `allcountries`. With no arguments, `Program.Main` runs a hardcoded default: the "Europe - West" area, all ages and then 10–40.

Runtime data goes in `./Data` (override with `-f`): `Data/Source/` holds the downloaded CSVs, `Data/EuropeanMortality.db` is the SQLite database, and the `.xlsx` outputs are written there too. The first run downloads and imports everything, which is slow. When `Data/Source` is more than 30 days old, it and the database are moved to `.old` and everything is rebuilt. Downloaders skip files that already exist, so delete a source file or the DB to force a refresh.

## Architecture

- **`Common/` is generic, the root level is European-specific.** `Common/` holds the mortality engine, DB layer, views, and CSV parser base. It's written to be reused by other country/region implementations (e.g. the default `OutputFile` is still `FrenchMortality.xlsx`). Keep region-specific logic out of `Common/`.
- **Options double as engine state.** Class hierarchy: `Options` → `MortalityEvolutionBase` (CLI options shared across verbs) → `MortalityTimeEvolution` (adds `TimeMode` and `RollingPeriod`) → `MortalityEvolution` (abstract engine) → `RollingEvolution`. Options are passed down with hand-written `CopyTo` methods, so **a new option must be added to the matching `CopyTo`**, or it will be silently dropped when `Program` builds per-country and per-time-mode engines.
- **Strategy for region specifics.** `MortalityEvolution` delegates the SQL query template, the country WHERE condition, display names, and period-length handling to a `SpecificImplementation` (`_Implementation`). `EuropeanMortalityEvolution` and `EuropeanRollingEvolution` (the latter is in `EuropeanVaccinationEvolution.cs`) just plug in an `EuropeanImplementation`.
- **Time modes.** `TimeMode` enum order matters: modes `<= Month` use `MortalityEvolution` + `MortalityEvolutionView`; `Week`/`Day` use `RollingEvolution` + `RollingEvolutionView` (rolling average). `Program.GenerateAllTimeModes` produces every mode as a separate sheet in the same workbook.
- **Generation pipeline** (`MortalityEvolution.Generate`): SQL aggregation of `DeathStatistics` → drop incomplete periods and normalize by period length → `Projection.BuildProjection` (regression baseline over `MinYearRegression`..`MaxYearRegression`; logarithmic unless some period has 0 deaths) → excess histogram and standard deviation → Z-score → optional vaccination left join → summary excess figures since `ExcessSince`.
- **Data layer.** `DatabaseEngine` is a thin ADO.NET wrapper. Tables are created by reflection from model classes in `Common/Model` (table name = type name + `s`, e.g. `DeathStatistic` → `DeathStatistics`). Parsers derive from `CsvParser`, map rows to model objects in `GetEntry`, and have an `IsBuilt` check so import only runs once. Deaths are age-standardized at import time (`StandardizedDeaths`) using `AgeStructure` against a reference year.
- **Views.** `BaseEvolutionView.Save` opens or creates the workbook at `Folder/OutputFile` and adds or replaces a sheet. Sheet and table names come from `BaseName` (country, time mode, ages, gender, injections), so names have to stay unique within a workbook. EPPlus runs with `LicenseContext.NonCommercial`.
- SQL is built with string formatting (no parameters). Country codes follow EuroStat (`EL` = Greece, mapped to `GR` for `RegionInfo`).
