using MortalityAnalyzer.Common;
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
    class AgeStructureLoader : CsvParser
    {
        public DataTable DataTable { get; private set; }
        const string SourceName = "demo_pjan";

        public AgeStructure AgeStructure { get; private set; }
        public void Load(string baseFolder)
        {
            AgeStructure = new AgeStructure(DatabaseEngine, MaxAge);
            try
            {
                AgeStructure.LoadAgeStructure();
                return;
            }
            catch { }
            Extract(baseFolder);
            AgeStructure.LoadAgeStructure();
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
        protected override DataTable CreateDataTable() => AgeStructure.CreateDataTable();

        public int MaxAge { get { return 100; } }
    }
}
