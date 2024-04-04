// See https://aka.ms/new-console-template for more information
using CommandLine;
using MortalityAnalyzer;
using MortalityAnalyzer.Common;
using MortalityAnalyzer.Downloaders;
using MortalityAnalyzer.Parser;
using MortalityAnalyzer.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length > 0)
            return Parser.Default.ParseArguments<MortalityEvolutionOptions, InitOptions, ShowOptions>(args)
                .MapResult(
                  (MortalityEvolutionOptions opts) => MortalityEvolution(opts),
                  (InitOptions opts) => Init(opts),
                  (ShowOptions opts) => Show(opts),
                  errs => 1);

        DatabaseEngine databaseEngine = Init();

        const int minAge = 10;
        const int maxAge = 40;
        MortalityEvolutionOptions mortalityEvolution = new MortalityEvolutionOptions { MinAge = minAge, MaxAge = maxAge, DisplayInjections = true, ToDateDelay = 50 };
        GenerateCountries(databaseEngine, mortalityEvolution, "Europe - West",  new string[] { "LU", "BE", "NL", "CH", "FR", "ES", "DK", "AT", "IT", "PT" });
        GenerateCountries(databaseEngine, mortalityEvolution, "Europe - East",  new string[] { "SK", "RO", "PO", "HU", "CZ", "BG" });
        GenerateCountries(databaseEngine, mortalityEvolution, "Europe - North", new string[] { "FI", "NO", "SE", "DK", });
        MortalityEvolution engine = mortalityEvolution.GetEvolutionEngine();
        engine.DatabaseEngine = databaseEngine;
        IEnumerable<string> countries = new EuropeanMortalityHelper(engine).GetSupportedCountries();
        //string[] countries = new string[] { "FR", "ES", "IT" };
        foreach (string country in countries)
            GenerateCountry(databaseEngine, mortalityEvolution, country);

        return 0;
    }

    private static void GenerateCountry(DatabaseEngine databaseEngine, MortalityEvolutionOptions mortalityEvolution, string country)
    {
        mortalityEvolution.Countries = new string[] { country };
        mortalityEvolution.Area = "";
        GenerateAllTimeModes(databaseEngine, mortalityEvolution);
    }
    private static void GenerateCountries(DatabaseEngine databaseEngine, MortalityEvolutionOptions mortalityEvolution, string area, string[] countries)
    {
        mortalityEvolution.Countries = countries;
        mortalityEvolution.Area = area;
        GenerateAllTimeModes(databaseEngine, mortalityEvolution);
    }

    //static void GenerateCountry(DatabaseEngine databaseEngine, string country, int minAge, int maxAge, DateTime? zoomMinDate = null, DateTime? zoomMaxDate = null, DateTime? excessSince = null)
    //{
    //    GenerateCountries(databaseEngine, new string[] { country }, null, minAge, maxAge, zoomMinDate, zoomMaxDate, excessSince);
    //}

    static void GenerateAllTimeModes(DatabaseEngine databaseEngine, MortalityEvolutionOptions mortalityEvolution)
    {
        GenerateTimeMode(databaseEngine, mortalityEvolution, TimeMode.Year);
        GenerateTimeMode(databaseEngine, mortalityEvolution, TimeMode.DeltaYear);
        GenerateTimeMode(databaseEngine, mortalityEvolution, TimeMode.Semester);
        GenerateTimeMode(databaseEngine, mortalityEvolution, TimeMode.Quarter);
        GenerateTimeMode(databaseEngine, mortalityEvolution, TimeMode.Month);
        GenerateTimeMode(databaseEngine, mortalityEvolution, TimeMode.Week, 8);
        GenerateTimeMode(databaseEngine, mortalityEvolution, TimeMode.Week, 4);
    }

    private static void GenerateTimeMode(DatabaseEngine databaseEngine, MortalityEvolutionOptions mortalityEvolution, TimeMode timeMode, int rollingPeriod = 7)
    {
        mortalityEvolution.TimeMode = timeMode;
        mortalityEvolution.RollingPeriod = rollingPeriod;
        MortalityEvolution engine = mortalityEvolution.GetEvolutionEngine();
        engine.DatabaseEngine = databaseEngine;
        Generate(engine);
    }

    private static void GenerateTimeMode(EuropeanMortalityEvolution mortalityEvolution, TimeMode timeMode)
    {
        mortalityEvolution.TimeMode = timeMode;
        Generate(mortalityEvolution);
    }

    private static void Generate(MortalityEvolution mortalityEvolution)
    {
        //mortalityEvolution.MinYearRegression = 2012;
        //mortalityEvolution.ToDateDelay = 50;
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
        return InitCore(new EuropeanMortalityEvolution());
    }

    private static DatabaseEngine InitCore(Options europeanMortalityEvolution)
    {
        return Init(europeanMortalityEvolution.Folder);
    }
    private static int Init(Options europeanMortalityEvolution)
    {
        InitCore(europeanMortalityEvolution);
        return 0;
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

    static int MortalityEvolution(MortalityEvolutionOptions mortalityEvolutionOptions)
    {
        DatabaseEngine databaseEngine = InitCore(mortalityEvolutionOptions);
        GenerateAllTimeModes(databaseEngine, mortalityEvolutionOptions);
        if (mortalityEvolutionOptions.Show)
            Show(mortalityEvolutionOptions);
        return 0;
    }

    private static DatabaseEngine GetDatabaseEngine(string dataFolder)
    {
        string databaseFile = Path.Combine(dataFolder, "EuropeanMortality.db");
        DatabaseEngine databaseEngine = new DatabaseEngine($"data source={databaseFile}", System.Data.SQLite.SQLiteFactory.Instance);
        databaseEngine.Connect();
        return databaseEngine;
    }
    private static int Show(Options initOptions)
    {
        string filePath = Path.Combine(initOptions.Folder, initOptions.OutputFile);
        if (File.Exists(filePath))
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        else
            Console.WriteLine("The file was not found!");
        return 0;
    }
}

