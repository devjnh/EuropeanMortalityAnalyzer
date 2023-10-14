using MortalityAnalyzer.Downloaders;
using MortalityAnalyzer.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Parser
{
    internal class EuroStatWeekly : CsvParser
    {
        public AgeStructure AgeStructure { get; set; }
        public bool FilesInserted { get; set; } = false;
        const string SourceName = "demo_r_mwk_05";
        public void Extract(string deathsLogFolder)
        {
            EuroStatDownloader euroStatDownloader = new EuroStatDownloader(deathsLogFolder);
            euroStatDownloader.Download(SourceName);
            Console.WriteLine("Importing mortality statistics");
            ImportFromCsvFile(Path.Combine(deathsLogFolder, $"{SourceName}.csv"));
        }

        protected override DataTable CreateDataTable()
        {
            return DatabaseEngine.CreateDataTable(typeof(DeathStatistic));
        }

        protected override object GetEntry(string[] split)
        {
            string age = split[3];
            string sex = split[4];
            string geo = split[6];
            string period = split[7];
            string count = split[8];
            if (sex != "T" || age == "TOTAL" || age == "UNK")
                return null;
            Regex regexWeek = new Regex("([0-9]{4})-W([0-9]{2})");
            var result = regexWeek.Match(period);
            if (!result.Success)
                return null;
            int year = Convert.ToInt32(result.Groups[1].Value);
            int week = Convert.ToInt32(result.Groups[2].Value);
            DateTime weekDate = FirstDateOfWeekISO8601(year, week);
            Regex regexAge = new Regex("Y([0-9]+)-([0-9]+)");
            result = regexAge.Match(age);
            if (!result.Success)
                return null;
            int minAge = Convert.ToInt32(result.Groups[1].Value);
            int maxAge = Convert.ToInt32(result.Groups[2].Value);
            int deaths = Convert.ToInt32(count);
            DeathStatistic deathStatistic = new DeathStatistic();
            deathStatistic.Age = minAge;
            deathStatistic.AgeSpan = maxAge + 1 - minAge;
            deathStatistic.Country = geo;
            deathStatistic.Date = weekDate.AddDays(3); // Use the date of the thursday (middle of the week)
            deathStatistic.Deaths = deaths;
            deathStatistic.Population = AgeStructure.GetPopulation(weekDate.Year, minAge, maxAge + 1, geo);
            deathStatistic.RefPopulation = AgeStructure.GetPopulation(AgeStructure.ReferenceYear, minAge, maxAge + 1, geo);
            deathStatistic.StandardizedDeaths = (double)deathStatistic.Deaths * deathStatistic.RefPopulation / deathStatistic.Population;

            return deathStatistic;
        }

        public bool IsBuilt => DatabaseEngine.DoesTableExist(DeathStatistic.StatisticsTableName);
    }
}
