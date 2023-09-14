// See https://aka.ms/new-console-template for more information
using EuropeanMortalityAnalyzer;
using EuropeanMortalityAnalyzer.Downloaders;
using EuropeanMortalityAnalyzer.Parser;
using EuropeanMortalityAnalyzer.Views;
using System.IO;

class Program
{
    static int Main(string[] args)
    {
        MortalityEvolution mortalityEvolution = new MortalityEvolution();
        if (!Directory.Exists(mortalityEvolution.Folder))
            Directory.CreateDirectory(mortalityEvolution.Folder);

        DatabaseEngine databaseEngine = GetDatabaseEngine(mortalityEvolution.Folder);
        AgeStructure ageStructure = new AgeStructure { DatabaseEngine = databaseEngine };
        ageStructure.Load(mortalityEvolution.Folder);
        EuroStatWeekly euroStatWeekly = new EuroStatWeekly { DatabaseEngine = databaseEngine, AgeStructure = ageStructure };
        if (!euroStatWeekly.IsBuilt)
            euroStatWeekly.Extract(mortalityEvolution.Folder);

        EcdcCovidVaxData owidCovidVaxData = new EcdcCovidVaxData { DatabaseEngine = databaseEngine};
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
            mortalityEvolution.Country = country;
            Generate(mortalityEvolution);
        }
        mortalityEvolution.Country = null;
        mortalityEvolution.Countries = new string[] { "LU", "BE", "NL", "CH", "FR", "ES", "DK", "AT", "IT" };
        Generate(mortalityEvolution);

        return 0;
    }

    private static void Generate(MortalityEvolution mortalityEvolution)
    {
        mortalityEvolution.Generate();
        MortalityEvolutionView mortalityEvolutionView = new MortalityEvolutionView { MortalityEvolution = mortalityEvolution };
        mortalityEvolutionView.Save();
    }

    private static DatabaseEngine GetDatabaseEngine(string dataFolder)
    {
        string databaseFile = Path.Combine(dataFolder, "EuropeanMortality.db");
        DatabaseEngine databaseEngine = new DatabaseEngine($"data source={databaseFile}", System.Data.SQLite.SQLiteFactory.Instance);
        databaseEngine.Connect();
        return databaseEngine;
    }
}

