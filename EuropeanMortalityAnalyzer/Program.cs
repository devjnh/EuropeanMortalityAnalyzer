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
            return Parser.Default.ParseArguments<MortalityEvolutionOptions, CountriesEvolutionOptions, AllCountriesEvolutionOptions, InitOptions, ShowOptions>(args)
                .MapResult(
                  (MortalityEvolutionOptions opts) => MortalityEvolution(opts),
                  (CountriesEvolutionOptions opts) => CountriesMortalityEvolution(opts),
                  (AllCountriesEvolutionOptions opts) => AllCountriesMortalityEvolution(opts),
                  (InitOptions opts) => Init(opts),
                  (ShowOptions opts) => Show(opts),
                  errs => 1);

        DatabaseEngine databaseEngine = Init();

        const int minAge = 10;
        const int maxAge = 40;
        MortalityEvolutionOptions mortalityEvolution = new MortalityEvolutionOptions { DisplayInjections = true, ToDateDelay = 50 };
        string[] countries = new string[] { "LU", "BE", "NL", "CH", "FR", "ES", "DK", "AT", "IT", "PT" };
        const string Area = "Europe - West";
        GenerateCountries(databaseEngine, mortalityEvolution, Area, countries);
        Show(mortalityEvolution);
        mortalityEvolution.MinAge = minAge;
        mortalityEvolution.MaxAge = maxAge;
        GenerateCountries(databaseEngine, mortalityEvolution, Area, countries);
        Show(mortalityEvolution);

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
        MortalityEvolution engine = mortalityEvolution.GetEvolutionEngine(timeMode, rollingPeriod);
        engine.DatabaseEngine = databaseEngine;
        Generate(engine);
    }

    private static void Generate(MortalityEvolution mortalityEvolution)
    {
        mortalityEvolution.Generate();
        BaseEvolutionView view = mortalityEvolution.TimeMode <= TimeMode.Month ? new EuropeanEvolutionView() : new EuropeanRollingEvolutionView();
        view.MortalityEvolution = mortalityEvolution;
        view.Save();
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

        EcdcCovidVaxData owidCovidVaxData = new EcdcCovidVaxData { DatabaseEngine = databaseEngine, Progress = ConsoleProgress.Instance };
        if (!owidCovidVaxData.IsBuilt)
            owidCovidVaxData.Extract(folder);

        EuroStatWeekly euroStatWeekly = new EuroStatWeekly { DatabaseEngine = databaseEngine, AgeStructure = ageStructureLoader.AgeStructure, Progress = ConsoleProgress.Instance };
        if (!euroStatWeekly.IsBuilt)
            euroStatWeekly.Extract(folder);

        return databaseEngine;
    }

    static int MortalityEvolution(MortalityEvolutionOptions mortalityEvolutionOptions)
    {
        DatabaseEngine databaseEngine = InitCore(mortalityEvolutionOptions);
        MortalityEvolution(mortalityEvolutionOptions, databaseEngine);
        return 0;
    }

    static void MortalityEvolution(MortalityEvolutionOptions mortalityEvolutionOptions, DatabaseEngine databaseEngine)
    {
        GenerateAllTimeModes(databaseEngine, mortalityEvolutionOptions);
        if (mortalityEvolutionOptions.Show)
            Show(mortalityEvolutionOptions);
    }

    static int CountriesMortalityEvolution(CountriesEvolutionOptions mortalityEvolutionOptions)
    {
        DatabaseEngine databaseEngine = InitCore(mortalityEvolutionOptions);
        CountriesMortalityEvolution(mortalityEvolutionOptions, mortalityEvolutionOptions.Countries, databaseEngine);
        return 0;
    }
    static int AllCountriesMortalityEvolution(AllCountriesEvolutionOptions mortalityEvolutionOptions)
    {
        DatabaseEngine databaseEngine = InitCore(mortalityEvolutionOptions);
        IEnumerable<string> countries = new EuropeanMortalityHelper(mortalityEvolutionOptions, databaseEngine).GetSupportedCountries();

        CountriesMortalityEvolution(mortalityEvolutionOptions, countries, databaseEngine);
        return 0;
    }

    private static void CountriesMortalityEvolution(MortalityEvolutionBase mortalityEvolutionOptions, IEnumerable<string> countries, DatabaseEngine databaseEngine)
    {
        foreach (string country in countries)
        {
            MortalityEvolutionOptions countryEvolutionOptions = new MortalityEvolutionOptions();
            mortalityEvolutionOptions.CopyTo(countryEvolutionOptions);
            countryEvolutionOptions.Countries = new string[] { country };
            MortalityEvolution(countryEvolutionOptions, databaseEngine);
        }
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
        string filePath = Path.Combine(initOptions.Folder, initOptions.ActualOutputFile);
        if (File.Exists(filePath))
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        else
            Console.WriteLine("The file was not found!");
        return 0;
    }
}

