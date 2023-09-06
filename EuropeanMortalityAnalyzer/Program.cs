// See https://aka.ms/new-console-template for more information
using EuropeanMortalityAnalyzer;
using EuropeanMortalityAnalyzer.Parser;
class Program
{
    static int Main(string[] args)
    {
        string folder = Directory.GetCurrentDirectory();
        DatabaseEngine databaseEngine = GetDatabaseEngine(folder);
        EuroStatWeekly euroStatWeekly = new EuroStatWeekly { DatabaseEngine = databaseEngine};
        euroStatWeekly.Extract(folder);
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

