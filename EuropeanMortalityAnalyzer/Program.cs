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
        Build(new EuropeanMortalityEvolution(), GenerateMortality);
        //Build(new EuropeanVaccinationEvolution(), GenerateVaccination);

        return 0;
    }

    private static void Build(MortalityEvolution mortalityEvolution, Action<MortalityEvolution> generationRoutine)
    {
        if (!Directory.Exists(mortalityEvolution.Folder))
            Directory.CreateDirectory(mortalityEvolution.Folder);

        DatabaseEngine databaseEngine = GetDatabaseEngine(mortalityEvolution.Folder);
        AgeStructure ageStructure = new AgeStructure { DatabaseEngine = databaseEngine };
        ageStructure.Load(mortalityEvolution.Folder);
        EuroStatWeekly euroStatWeekly = new EuroStatWeekly { DatabaseEngine = databaseEngine, AgeStructure = ageStructure };
        if (!euroStatWeekly.IsBuilt)
            euroStatWeekly.Extract(mortalityEvolution.Folder);

        EcdcCovidVaxData owidCovidVaxData = new EcdcCovidVaxData { DatabaseEngine = databaseEngine };
        if (!owidCovidVaxData.IsBuilt)
            owidCovidVaxData.Extract(mortalityEvolution.Folder);

        mortalityEvolution.DatabaseEngine = databaseEngine;
        mortalityEvolution.TimeMode = TimeMode.Quarter;
        mortalityEvolution.MinAge = 5;
        mortalityEvolution.MaxAge = 40;
        mortalityEvolution.MinYearRegression = 2014;
        mortalityEvolution.OutputFile = $"EuropeanMortality {mortalityEvolution.MinAge}-{mortalityEvolution.MaxAge}.xlsx";
        string[] countries = new string[] { "FR", "ES", "IT" };
        foreach (string country in countries)
        {
            ((EuropeanImplementation)mortalityEvolution.Implementation).Country = country;
            generationRoutine((MortalityEvolution)mortalityEvolution);
        }
        ((EuropeanImplementation)mortalityEvolution.Implementation).Country = null;
        ((EuropeanImplementation)mortalityEvolution.Implementation).Countries = new string[] { "LU", "BE", "NL", "CH", "FR", "ES", "DK", "AT", "IT" };
        generationRoutine((MortalityEvolution)mortalityEvolution);
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
        VaccinationEvolutionView vaccinationEvolutionView = new VaccinationEvolutionView { MortalityEvolution = (VaccinationEvolution)mortalityEvolution };
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

