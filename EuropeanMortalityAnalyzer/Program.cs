﻿// See https://aka.ms/new-console-template for more information
using MortalityAnalyzer;
using MortalityAnalyzer.Downloaders;
using MortalityAnalyzer.Parser;
using MortalityAnalyzer.Views;
using System;
using System.Collections.Generic;
using System.IO;

class Program
{
    static int Main(string[] args)
    {
        DatabaseEngine databaseEngine = Init();

        GenerateAgeRange(databaseEngine, 5, 40);
        GenerateAgeRange(databaseEngine, 5, 10, new DateTime(2021, 1, 1), new DateTime(2023, 7, 1), new DateTime(2022, 1, 1));

        return 0;
    }

    private static void GenerateAgeRange(DatabaseEngine databaseEngine, int minAge, int maxAge, DateTime? zoomMinDate = null, DateTime? zoomMaxDate = null, DateTime? excessSince = null)
    {
        EuropeanMortalityEvolution mortalityEvolution = new EuropeanMortalityEvolution();
        ConfigureCommon(mortalityEvolution, databaseEngine, minAge, maxAge);
        if (excessSince != null)
            mortalityEvolution.ExcessSince = excessSince.Value;
        GenerateEvolution(mortalityEvolution, TimeMode.Year);
        GenerateEvolution(mortalityEvolution, TimeMode.DeltaYear);
        GenerateEvolution(mortalityEvolution, TimeMode.Semester);
        GenerateEvolution(mortalityEvolution, TimeMode.Quarter);
        GenerateEvolution(mortalityEvolution, TimeMode.Month);

        EuropeanRollingEvolution rollingEvolution = new EuropeanRollingEvolution();
        if (zoomMinDate != null)
            rollingEvolution.ZoomMinDate = zoomMinDate.Value;
        if (zoomMaxDate != null)
            rollingEvolution.ZoomMaxDate = zoomMaxDate.Value;
        ConfigureCommon(rollingEvolution, databaseEngine, minAge, maxAge);

        rollingEvolution.RollingPeriod = 8;
        Build(rollingEvolution);

        rollingEvolution.RollingPeriod = 4;
        Build(rollingEvolution);
    }

    private static void ConfigureCommon(MortalityEvolution mortalityEvolution, DatabaseEngine databaseEngine, int minAge, int maxAge)
    {
        mortalityEvolution.DisplayInjections = true;
        mortalityEvolution.MinAge = minAge;
        mortalityEvolution.MaxAge = maxAge;
        mortalityEvolution.DatabaseEngine = databaseEngine;
    }

    private static void GenerateEvolution(EuropeanMortalityEvolution mortalityEvolution, TimeMode timeMode)
    {
        mortalityEvolution.TimeMode = timeMode;
        Build(mortalityEvolution);
    }


    private static void Build(MortalityEvolution mortalityEvolution)
    {
        EuropeanImplementation europeanImplementation = ((EuropeanImplementation)mortalityEvolution.Implementation);

        IEnumerable<string> countries = new EuropeanMortalityHelper(mortalityEvolution).GetSupportedCountries();
        //string[] countries = new string[] { "FR" };
        foreach (string country in countries)
        {
            europeanImplementation.Country = country;
            europeanImplementation.Area = null;
            Generate(mortalityEvolution);
        }
        europeanImplementation.Countries = new string[] { "SK", "RO", "PO", "HU", "CZ", "BG" };
        europeanImplementation.Area = "Europe - East";
        Generate(mortalityEvolution);
        europeanImplementation.Countries = new string[] { "FI", "NO", "SE", "DK", };
        europeanImplementation.Area = "Europe - North";
        Generate(mortalityEvolution);
        europeanImplementation.Countries = new string[] { "LU", "BE", "NL", "CH", "AT", "PT" };
        europeanImplementation.Area = "Europe - Other";
        Generate(mortalityEvolution);
        europeanImplementation.Countries = new string[] { "LU", "BE", "NL", "CH", "FR", "ES", "DK", "AT", "IT", "PT" };
        europeanImplementation.Area = "Europe - West";
        Generate(mortalityEvolution);
    }

    private static void Generate(MortalityEvolution mortalityEvolution)
    {
        mortalityEvolution.MinYearRegression = 2012;
        mortalityEvolution.OutputFile = $"{GetArea(mortalityEvolution)} {mortalityEvolution.MinAge}-{mortalityEvolution.MaxAge}.xlsx";
        mortalityEvolution.Generate();
        BaseEvolutionView view = mortalityEvolution.TimeMode <= TimeMode.Month ? new MortalityEvolutionView() : new RollingEvolutionView();
        view.MortalityEvolution = mortalityEvolution;
        view.Save();
    }

    private static string GetArea(MortalityEvolution mortalityEvolution)
    {
        return string.IsNullOrWhiteSpace(mortalityEvolution.CountryName) ? "Europe" : mortalityEvolution.CountryName;
    }

    private static DatabaseEngine Init()
    {
        return Init(new EuropeanMortalityEvolution().Folder);
    }

    private static DatabaseEngine Init(string folder)
    {
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        DatabaseEngine databaseEngine = GetDatabaseEngine(folder);
        AgeStructureLoader ageStructureLoader = new AgeStructureLoader { DatabaseEngine = databaseEngine, Progress = ConsoleProgress.Instance };
        ageStructureLoader.Load(folder);
        EuroStatWeekly euroStatWeekly = new EuroStatWeekly { DatabaseEngine = databaseEngine, AgeStructure = ageStructureLoader.AgeStructure, Progress = ConsoleProgress.Instance };
        if (!euroStatWeekly.IsBuilt)
            euroStatWeekly.Extract(folder);

        EcdcCovidVaxData owidCovidVaxData = new EcdcCovidVaxData { DatabaseEngine = databaseEngine, Progress = ConsoleProgress.Instance };
        if (!owidCovidVaxData.IsBuilt)
            owidCovidVaxData.Extract(folder);

        return databaseEngine;
    }

    private static DatabaseEngine GetDatabaseEngine(string dataFolder)
    {
        string databaseFile = Path.Combine(dataFolder, "EuropeanMortality.db");
        DatabaseEngine databaseEngine = new DatabaseEngine($"data source={databaseFile}", System.Data.SQLite.SQLiteFactory.Instance);
        databaseEngine.Connect();
        return databaseEngine;
    }
}

