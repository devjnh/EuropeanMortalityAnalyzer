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
    internal class EcdcCovidVaxData : CsvParser
    {

        public void Extract(string deathsLogFolder)
        {
            FileDownloader fileDownloader = new FileDownloader(deathsLogFolder);
            string fileName = "EcdcCovidVaxData.csv";
            fileDownloader.Download(fileName, "https://opendata.ecdc.europa.eu/covid19/vaccine_tracker/csv/data.csv");
            Console.WriteLine("Importing Covid vaccination statistics");
            ImportFromCsvFile(Path.Combine(deathsLogFolder, fileName));
        }

        protected override DataTable CreateDataTable()
        {
            return DatabaseEngine.CreateDataTable(typeof(VaxStatistic));
        }

        protected override object GetEntry(string[] split)
        {
            VaxStatistic vaxStatistic = new VaxStatistic();
            Regex regexWeek = new Regex("([0-9]{4})-W([0-9]{2})");
            var result = regexWeek.Match(GetValue("YearWeekISO", split));
            if (!result.Success)
                return null;
            int year = Convert.ToInt32(result.Groups[1].Value);
            int week = Convert.ToInt32(result.Groups[2].Value);
            vaxStatistic.Date = FirstDateOfWeekISO8601(year, week);
            vaxStatistic.Country = GetValue("ReportingCountry", split);
            string region = GetValue("Region", split);
            if (region != vaxStatistic.Country)
                return null;
            vaxStatistic.Vaccine = GetValue("Vaccine", split);
            vaxStatistic.D1 = GetIntValue("FirstDose", split);
            vaxStatistic.D2 = GetIntValue("SecondDose", split);
            vaxStatistic.D3 = GetIntValue("DoseAdditional1", split);
            vaxStatistic.Population = GetIntValue("Population", split);
            string ageGroup = GetValue("TargetGroup", split);
            Regex regexAge = new Regex("Age([0-9]+)_([0-9]+)");
            result = regexAge.Match(ageGroup);
            if (result.Success)
            {
                vaxStatistic.Age = Convert.ToInt32(result.Groups[1].Value);
                int upperAge = Convert.ToInt32(result.Groups[2].Value);
                vaxStatistic.AgeSpan = upperAge - vaxStatistic.Age + 1;
                return vaxStatistic;
            }
            regexAge = new Regex("Age<([0-9]+)");
            result = regexAge.Match(ageGroup);
            if (result.Success)
            {
                vaxStatistic.Age = 0;
                int upperAge = Convert.ToInt32(result.Groups[1].Value);
                vaxStatistic.AgeSpan = upperAge - vaxStatistic.Age;
                return vaxStatistic;
            }
            regexAge = new Regex("Age([0-9]+)+");
            result = regexAge.Match(ageGroup);
            if (result.Success)
            {
                vaxStatistic.Age = Convert.ToInt32(result.Groups[1].Value);
                vaxStatistic.AgeSpan = -1;
                return vaxStatistic;
            }

            return null;
        }
        public bool IsBuilt => DatabaseEngine.DoesTableExist(VaxStatistic.StatisticsTableName);
    }
}
