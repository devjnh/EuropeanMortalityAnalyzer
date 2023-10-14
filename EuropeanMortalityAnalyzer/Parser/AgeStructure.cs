using MortalityAnalyzer.Common.Model;
using MortalityAnalyzer.Downloaders;
using MortalityAnalyzer.Model;
using MortalityAnalyzer.Parser;
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
    class AgeStructure : CsvParser
    {
        static public int ReferenceYear { get; set; } = 2022;
        public DataTable DataTable { get; private set; }
        const string SourceName = "demo_pjan";

        static AgeStructure()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public void Load(string baseFolder)
        {
            try
            {
                LoadAgeStructure();
                return;
            }
            catch { }
            Extract(baseFolder);
            LoadAgeStructure();
        }

        private void LoadAgeStructure()
        {
            DataTable = CreateDataTable();
            Console.WriteLine($"Loading age structure");
            DatabaseEngine.FillDataTable("SELECT Year, Age, Population, Gender, Country FROM AgeStructure ORDER BY Year, Age", DataTable);
            Console.WriteLine($"Age structure loaded");
        }

        private void Extract(string baseFolder)
        {
            EuroStatDownloader euroStatDownloader = new EuroStatDownloader(baseFolder);
            euroStatDownloader.Download(SourceName);
            Console.WriteLine($"Extracting age structure");
            ImportFromCsvFile(Path.Combine(baseFolder, $"{SourceName}.csv"));
            Console.WriteLine($"Age structure inserted");
        }

        protected override object GetEntry(string[] split)
        {
            string age = split[4];
            string sex = split[5];
            string geo = split[6];
            string period = split[7];
            string count = split[8];
            if (age == "TOTAL" || age == "UNK")
                return null;
            Regex regexAge = new Regex("Y([0-9]+)");
            var result = regexAge.Match(age);
            if (!result.Success)
                return null;
            GenderFilter gender = sex == "F" ? GenderFilter.Female : sex == "M" ? GenderFilter.Male : GenderFilter.All;
            AgeStatistic ageStatistic = new AgeStatistic();
            ageStatistic.Year = Convert.ToInt32(period);
            ageStatistic.Age = Convert.ToInt32(result.Groups[1].Value);
            ageStatistic.Population = String.IsNullOrWhiteSpace(count) ? 0 : Convert.ToInt32(count);
            ageStatistic.Gender = gender;
            ageStatistic.Country = geo;

            return ageStatistic;
        }
        protected override DataTable CreateDataTable()
        {
            DataTable  dataTable = DatabaseEngine.CreateDataTable(typeof(AgeStatistic), "AgeStructure");
            dataTable.PrimaryKey = new DataColumn[] { dataTable.Columns[nameof(AgeStatistic.Year)], dataTable.Columns[nameof(AgeStatistic.Age)], dataTable.Columns[nameof(AgeStatistic.Gender)], dataTable.Columns[nameof(AgeStatistic.Country)] };
            return dataTable;
        }

        public int MaxAge { get { return 100; } }

        public int GetPopulation(int year, int age, string country, GenderFilter genderFilter = GenderFilter.All)
        {
            int ageLowerBound = age <= MaxAge ? age : MaxAge;
            DataRow[] rows = DataTable.Select($"Year={year} AND Age={ageLowerBound} AND Gender={(int)genderFilter} AND Country = {country}");
            return (int)rows[0][nameof(AgeStatistic.Population)];
        }
        public int GetPopulation(int year, int minAge, int maxAge, string country, GenderFilter genderFilter = GenderFilter.All)
        {
            if (year > 2022)
                year = 2022;
            DataRow[] rows = DataTable.Select($"Year={year} AND Age>={minAge}  AND Age<{maxAge} AND Gender={(int)genderFilter} AND Country = '{country}'");
            return rows.Sum(r => (int)rows[0][nameof(AgeStatistic.Population)]);
        }
    }
}
