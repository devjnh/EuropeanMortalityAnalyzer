using MortalityAnalyzer.Common.Model;
using MortalityAnalyzer.Downloaders;
using MortalityAnalyzer.Model;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    class AgeStructure
    {
        static public int ReferenceYear { get; set; } = 2022;
        public DataTable DataTable { get; private set; }
        public DatabaseEngine DatabaseEngine { get; set; }
        const string SourceName = "demo_pjan";

        static AgeStructure()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public void Load(string baseFolder)
        {
            DataTable = DatabaseEngine.CreateDataTable(typeof(AgeStatistic), "AgeStructure");
            DataTable.PrimaryKey = new DataColumn[] { DataTable.Columns[nameof(AgeStatistic.Year)], DataTable.Columns[nameof(AgeStatistic.Age)], DataTable.Columns[nameof(AgeStatistic.Gender)], DataTable.Columns[nameof(AgeStatistic.Country)] };

            Console.WriteLine($"Loading age structure");
            try
            {
                DatabaseEngine.FillDataTable("SELECT Year, Age, Population, Gender, Country FROM AgeStructure ORDER BY Year, Age", DataTable);
                Console.WriteLine($"Age structure loaded");
                return;
            }
            catch { }
            Extract(baseFolder);
        }

        private void Extract(string baseFolder)
        {
            EuroStatDownloader euroStatDownloader = new EuroStatDownloader(baseFolder);
            euroStatDownloader.Download(SourceName);
            Console.WriteLine($"Extracting age structure");
            DatabaseEngine.Prepare(DataTable);

            using (FileStream fileStream = new FileStream(Path.Combine(baseFolder, $"{SourceName}.csv"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader textReader = new StreamReader(fileStream))
                {
                    string line = textReader.ReadLine();
                    string[] header = line.Split(',');
                    while (!textReader.EndOfStream)
                    {
                        line = textReader.ReadLine();
                        string[] split = line.Split(',');
                        string age = split[4];
                        string sex = split[5];
                        string geo = split[6];
                        string period = split[7];
                        string count = split[8];
                        if (age == "TOTAL" || age == "UNK")
                            continue;
                        Regex regexAge = new Regex("Y([0-9]+)");
                        var result = regexAge.Match(age);
                        if (!result.Success)
                            continue;
                        GenderFilter gender = sex == "F" ? GenderFilter.Female : sex == "M" ? GenderFilter.Male : GenderFilter.All;
                        AgeStatistic ageStatistic = new AgeStatistic();
                        ageStatistic.Year = Convert.ToInt32(period);
                        ageStatistic.Age = Convert.ToInt32(result.Groups[1].Value);
                        ageStatistic.Population = String.IsNullOrWhiteSpace(count) ? 0 : Convert.ToInt32(count);
                        ageStatistic.Gender = gender;
                        ageStatistic.Country = geo;
                        DatabaseEngine.Insert(ageStatistic);
                        //Regex regexWeek = new Regex("([0-9]{4})-W([0-9]{2})");
                        //var result = regexWeek.Match(period);
                        //if (!result.Success)
                        //    continue;
                        //int year = Convert.ToInt32(result.Groups[1].Value);
                        //int week = Convert.ToInt32(result.Groups[2].Value);
                        //DateTime weekDate = FirstDateOfWeekISO8601(year, week);
                    }
                }
            }
            DatabaseEngine.FinishInsertion();
            Console.WriteLine($"Age structure inserted");
        }

        public int MaxAge { get { return 100; } }

        public int GetPopulation(int year, int age, string country, GenderFilter genderFilter = GenderFilter.All)
        {
            int ageLowerBound = age <= MaxAge ? age : MaxAge;
            DataRow[] rows = DataTable.Select($"Year={year} AND Age={ageLowerBound} AND Gender={(int)genderFilter} AND Country = {country}");
            return (int)rows[0][2];
        }
        public int GetPopulation(int year, int minAge, int maxAge, string country, GenderFilter genderFilter = GenderFilter.All)
        {
            if (year > 2022)
                year = 2022;
            DataRow[] rows = DataTable.Select($"Year={year} AND Age>={minAge}  AND Age<{maxAge} AND Gender={(int)genderFilter} AND Country = '{country}'");
            return rows.Sum(r => (int)rows[0][2]);
        }
    }
}
