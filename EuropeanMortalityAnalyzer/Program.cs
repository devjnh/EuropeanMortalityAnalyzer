// See https://aka.ms/new-console-template for more information
using EuropeanMortalityAnalyzer;
using EuropeanMortalityAnalyzer.Parser;
using EuropeanMortalityAnalyzer.Views;

class Program
{
    static int Main(string[] args)
    {
        string folder = Directory.GetCurrentDirectory();
        DatabaseEngine databaseEngine = GetDatabaseEngine(folder);
        AgeStructure ageStructure = new AgeStructure { DatabaseEngine = databaseEngine};
        ageStructure.Load(folder);
        EuroStatWeekly euroStatWeekly = new EuroStatWeekly { DatabaseEngine = databaseEngine, AgeStructure = ageStructure };
        if (!euroStatWeekly.IsBuilt)
            euroStatWeekly.Extract(folder);
        MortalityEvolution mortalityEvolution = new MortalityEvolution { DatabaseEngine = databaseEngine};
        mortalityEvolution.TimeMode = TimeMode.Semester;
        mortalityEvolution.MinAge = 5;
        mortalityEvolution.MaxAge = 40;
        mortalityEvolution.MinYearRegression = 2012;
        string[] countries = new string[] { "FR", "ES", "IT" };
        foreach (string country in countries)
        {
            mortalityEvolution.Country = country;
            mortalityEvolution.Generate();
            MortalityEvolutionView mortalityEvolutionView = new MortalityEvolutionView { MortalityEvolution = mortalityEvolution};
            mortalityEvolutionView.Save();
        }
        return 0;
    }
    private static DatabaseEngine GetDatabaseEngine(string dataFolder)
    {
        string databaseFile = Path.Combine(dataFolder, "EuropeanMortality.db");
        DatabaseEngine databaseEngine = new DatabaseEngine($"data source={databaseFile}", System.Data.SQLite.SQLiteFactory.Instance);
        databaseEngine.Connect();
        return databaseEngine;
    }
}

