# EuropeanMortalityAnalyzer

EuropeanMortalityAnalyzer builds mortality statistics for European countries from the weekly death counts published by EuroStat. It generates Excel spreadsheets with charts showing:

- Age-standardized deaths by year, July–June year, semester, quarter, month and week (rolling average)
- A baseline projection computed by regression over a reference period (2012–2020 by default)
- Excess mortality relative to that baseline, its distribution, and the corresponding Z-score
- Optionally, Covid-19 vaccine injections alongside the mortality curves

Results can be filtered by country or group of countries, by age range and by gender.

## Data sources

On first run the program downloads:

- Weekly deaths by sex and 5-year age group from [EuroStat (demo_r_mwk_05)](https://ec.europa.eu/eurostat/databrowser/view/DEMO_R_MWK_05/default/table?lang=en&category=demo.demomwk "Deaths by week, sex and 5-year age group")
- Population on 1 January by age and sex from [EuroStat (demo_pjan)](https://ec.europa.eu/eurostat/databrowser/view/demo_pjan/default/table?lang=en "Population on 1 January by age and sex")
- Covid-19 vaccination data from the [ECDC](https://www.ecdc.europa.eu/en/publications-data/data-covid-19-vaccination-eu-eea "Data on COVID-19 vaccination in the EU/EEA")

Deaths are standardized according to the age structure of each country, using 2022 as the reference year, so that periods with different population sizes and age pyramids can be compared.

## How to use the executable

Download the prebuilt Windows executable:
[Download EuropeanMortality.zip](https://github.com/devjnh/EuropeanMortalityAnalyzer/releases/latest/download/EuropeanMortality.zip)

Extract the zip file into a new folder and launch `EuropeanMortalityAnalyzer.exe`.

### First run

The first run:

1. Creates a `Data` subfolder in the working folder, where all files are stored
2. Downloads the source files listed above into `Data/Source`
3. Imports them into a SQLite database, `Data/EuropeanMortality.db`
4. Calculates weekly age-standardized death statistics by 5-year age group

Downloading and importing takes a while, so please be patient. Later runs reuse the database. When the downloaded data is more than 30 days old, it is archived (as `Source.old` and `EuropeanMortality.old.db`) and fresh data is downloaded automatically.

### Default output

Run without arguments, the program generates these files in the `Data` folder:

- `Europe - West.xlsx`: mortality of a group of Western European countries (LU, BE, NL, CH, FR, ES, DK, AT, IT, PT)
- `Europe - West 10-40.xlsx`: the same for ages 10 to 40

Each workbook contains one sheet per time granularity: year, July–June year, semester, quarter, month, and week with 8-week and 4-week rolling averages.

### Command line

The program supports these commands:

| Command | Description |
| --- | --- |
| `countries` | One spreadsheet per country listed with `--Countries` |
| `area` | One spreadsheet for a group of countries (`--Countries`), named after `--Area` |
| `allcountries` | One spreadsheet for every country with recent enough EuroStat data |
| `init` | Only download and import the data into the database |
| `show` | Open a generated spreadsheet (`-o`) |
| `help` | Show the available commands; `help <command>` shows that command's options |

Examples:

    EuropeanMortalityAnalyzer.exe countries --Countries FR ES IT
    EuropeanMortalityAnalyzer.exe countries --Countries FR ES IT --MinAge 5 --MaxAge 40
    EuropeanMortalityAnalyzer.exe area --Countries FR ES IT --Area "France Italy Spain" --MinAge 5 --MaxAge 40 --Show

They generate:

- `France.xlsx`, `Spain.xlsx`, `Italy.xlsx`: mortality of each country
- `France 5-40.xlsx`, `Spain 5-40.xlsx`, `Italy 5-40.xlsx`: the same for ages 5 to 40
- `France Italy Spain 5-40.xlsx`: mortality of the three countries together for ages 5 to 40, opened in Excel at the end

Countries use EuroStat codes, which are mostly ISO 3166 codes, except `EL` for Greece.

Main options (option names are case-sensitive):

| Option | Description | Default |
| --- | --- | --- |
| `--Countries` | Countries to include | |
| `--Area` | Display name of the group of countries (`area` command) | |
| `--MinAge`, `--MaxAge` | Age range included in the analysis | all ages |
| `-g`, `--Gender` | `All`, `Male` or `Female` | `All` |
| `--MinYearRegression`, `--MaxYearRegression` | Years used for the baseline regression | 2012, 2020 |
| `--ExcessSince` | Start date used for the total excess calculation | 2021-07-01 |
| `-i`, `--Injections` | Show Covid-19 vaccine injections | off |
| `-r`, `--Raw` | Also show raw (non-standardized) deaths | off |
| `-d`, `--Delay` | Days to ignore at the end of the data, as the last weeks are often incomplete | 30 |
| `--ZoomMinDate`, `--ZoomMaxDate` | Time range of the zoomed weekly charts | 2020-01-01, 2022-07-01 |
| `-f`, `--folder` | Data folder | `./Data` |
| `-o`, `--OutputFile` | Output spreadsheet name, overriding the automatic name | |
| `--Show` | Open the spreadsheet in Excel when done | off |

## How to build the executable

The project targets .NET Framework 4.7.2 (Windows) and .NET 6.0.

With Visual Studio 2022 (the free [community edition](https://visualstudio.microsoft.com/vs/community/) is fine), open `EuropeanMortalityAnalyzer.sln` and build it.

With the .NET SDK on the command line:

    dotnet build EuropeanMortalityAnalyzer.sln
    dotnet run --project EuropeanMortalityAnalyzer -f net6.0 -- countries --Countries FR

The executables are generated in `EuropeanMortalityAnalyzer\bin\<Configuration>\net472` and `EuropeanMortalityAnalyzer\bin\<Configuration>\net6.0`.

## How to distribute the executable

On Windows, copy `EuropeanMortalityAnalyzer.exe` together with all the DLL files (and the `x86`/`x64` subfolders containing the SQLite native library) from the `net472` output folder.

On Linux or macOS, you can try the .NET 6.0 build, which hasn't been tested yet.

## License

See [LICENSE](LICENSE).
