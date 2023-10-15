// See https://aka.ms/new-console-template for more information
using MortalityAnalyzer;
using MortalityAnalyzer.Downloaders;
using MortalityAnalyzer.Parser;
using MortalityAnalyzer.Views;
using System;
using System.IO;

class Program
{
    static int Main(string[] args)
    {
        DatabaseEngine databaseEngine = Init();

        GenerateAgeRange(databaseEngine, 5, 40);
        GenerateAgeRange(databaseEngine, 5, 10, new DateTime(2021, 1, 1), new DateTime(2023, 7, 1));

        return 0;
    }

    private static void GenerateAgeRange(DatabaseEngine databaseEngine, int minAge, int maxAge, DateTime? zoomMinDate = null, DateTime? zoomMaxDate = null)
    {
        EuropeanMortalityEvolution mortalityEvolution = new EuropeanMortalityEvolution();
        ConfigureCommon(mortalityEvolution, databaseEngine, minAge, maxAge);
        //GenerateEvolution(mortalityEvolution, TimeMode.Year);
        //GenerateEvolution(mortalityEvolution, TimeMode.DeltaYear);
        GenerateEvolution(mortalityEvolution, TimeMode.Semester);
        GenerateEvolution(mortalityEvolution, TimeMode.Quarter);

        EuropeanRollingEvolution rollingEvolution = new EuropeanRollingEvolution();
        if (zoomMinDate != null)
            rollingEvolution.ZoomMinDate = zoomMinDate.Value;
        if (zoomMaxDate != null)
            rollingEvolution.ZoomMaxDate = zoomMaxDate.Value;
        ConfigureCommon(rollingEvolution, databaseEngine, minAge, maxAge);

        rollingEvolution.RollingPeriod = 8;
        GenerateAllDoses(rollingEvolution);

        //rollingEvolution.RollingPeriod = 4;
        //GenerateAllDoses(rollingEvolution);
    }

    private static void ConfigureCommon(MortalityEvolution mortalityEvolution, DatabaseEngine databaseEngine, int minAge, int maxAge)
    {
        mortalityEvolution.MinAge = minAge;
        mortalityEvolution.MaxAge = maxAge;
        mortalityEvolution.DatabaseEngine = databaseEngine;
    }

    private static void GenerateEvolution(EuropeanMortalityEvolution mortalityEvolution, TimeMode timeMode)
    {
        mortalityEvolution.TimeMode = timeMode;
        GenerateAllDoses(mortalityEvolution);
    }

    private static void GenerateAllDoses(MortalityEvolution mortalityEvolution)
    {
        foreach (VaxDose vaxDose in Enum.GetValues(typeof(VaxDose)))
        {
            if (vaxDose == VaxDose.None)
                continue;
            mortalityEvolution.Injections = vaxDose;
            Build(mortalityEvolution);
        }
    }

    private static void Build(MortalityEvolution mortalityEvolution)
    {
        mortalityEvolution.MinYearRegression = 2014;
        mortalityEvolution.OutputFile = $"EuropeanMortality {mortalityEvolution.MinAge}-{mortalityEvolution.MaxAge}-{mortalityEvolution.TimeMode}-{mortalityEvolution.Injections}.xlsx";
        string[] countries = new string[] { "FR", "ES", "IT" };
        foreach (string country in countries)
        {
            ((EuropeanImplementation)mortalityEvolution.Implementation).Country = country;
            Generate(mortalityEvolution);
        }
        ((EuropeanImplementation)mortalityEvolution.Implementation).Country = null;
        ((EuropeanImplementation)mortalityEvolution.Implementation).Countries = new string[] { "LU", "BE", "NL", "CH", "FR", "ES", "DK", "AT", "IT", "PT" };
        Generate(mortalityEvolution);
    }

    private static void Generate(MortalityEvolution mortalityEvolution)
    {
        if (mortalityEvolution is RollingEvolution)
            GenerateVaccination(mortalityEvolution);
        else
            GenerateMortality(mortalityEvolution);
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
        AgeStructureLoader ageStructureLoader = new AgeStructureLoader { DatabaseEngine = databaseEngine };
        ageStructureLoader.Load(folder);
        EuroStatWeekly euroStatWeekly = new EuroStatWeekly { DatabaseEngine = databaseEngine, AgeStructure = ageStructureLoader.AgeStructure };
        if (!euroStatWeekly.IsBuilt)
            euroStatWeekly.Extract(folder);

        EcdcCovidVaxData owidCovidVaxData = new EcdcCovidVaxData { DatabaseEngine = databaseEngine };
        if (!owidCovidVaxData.IsBuilt)
            owidCovidVaxData.Extract(folder);

        return databaseEngine;
    }

    private static void GenerateMortality(MortalityEvolution mortalityEvolution)
    {
        mortalityEvolution.Generate();
        MortalityEvolutionView mortalityEvolutionView = new MortalityEvolutionView { MortalityEvolution = mortalityEvolution };
        mortalityEvolutionView.Save();
    }
    private static void GenerateVaccination(MortalityEvolution mortalityEvolution)
    {
        mortalityEvolution.Generate();
        RollingEvolutionView vaccinationEvolutionView = new RollingEvolutionView { MortalityEvolution = mortalityEvolution };
        vaccinationEvolutionView.Save();
    }

    private static DatabaseEngine GetDatabaseEngine(string dataFolder)
    {
        string databaseFile = Path.Combine(dataFolder, "EuropeanMortality.db");
        DatabaseEngine databaseEngine = new DatabaseEngine($"data source={databaseFile}", System.Data.SQLite.SQLiteFactory.Instance);
        databaseEngine.Connect();
        return databaseEngine;
    }
}

