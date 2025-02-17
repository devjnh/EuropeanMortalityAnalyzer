﻿using MortalityAnalyzer.Common;
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
        public GenderFilter GenderFilter { get; set; } = GenderFilter.All;
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
            if (age == "TOTAL" || age == "UNK")
                return null;
            GenderFilter gender = GetGender(sex);
            if (gender != GenderFilter)
                return null;
            Regex regexWeek = new Regex("([0-9]{4})-W([0-9]{2})");
            var result = regexWeek.Match(period);
            if (!result.Success)
                return null;
            int year = Convert.ToInt32(result.Groups[1].Value);
            int week = Convert.ToInt32(result.Groups[2].Value);
            DateTime weekDate = FirstDateOfWeekISO8601(year, week);
            int minAge;
            int maxAge;
            Regex regexAge = new Regex("Y([0-9]+)-([0-9]+)");
            result = regexAge.Match(age);
            if (result.Success)
            {
                minAge = Convert.ToInt32(result.Groups[1].Value);
                maxAge = Convert.ToInt32(result.Groups[2].Value);
            }
            else
            {
                regexAge = new Regex("Y_(GE|LT)([0-9]+)");
                result = regexAge.Match(age);
                if (result.Success)
                {
                    if (result.Groups[1].Value == "LT")
                    {
                        maxAge = Convert.ToInt32(result.Groups[2].Value) - 1;
                        minAge = 0;
                    }
                    else
                    {

                        minAge = Convert.ToInt32(result.Groups[2].Value);
                        maxAge = -1;
                    }
                }
                else
                    return null;
            }
            int deaths = string.IsNullOrWhiteSpace(count) ? 0 : Convert.ToInt32(count);
            DeathStatistic deathStatistic = new DeathStatistic();
            deathStatistic.Age = minAge;
            deathStatistic.AgeSpan = maxAge == -1 ? -1 : maxAge + 1 - minAge;
            deathStatistic.Gender = gender;
            deathStatistic.Country = geo;
            deathStatistic.Date = weekDate.AddDays(3); // Use the date of the thursday (middle of the week)
            deathStatistic.Deaths = deaths;
            deathStatistic.Population = AgeStructure.GetPopulation(weekDate.Year, minAge, maxAge + 1, geo);
            deathStatistic.RefPopulation = AgeStructure.GetPopulation(AgeStructure.ReferenceYear, minAge, maxAge + 1, geo);
            deathStatistic.StandardizedDeaths = (double)deathStatistic.Deaths * deathStatistic.RefPopulation / deathStatistic.Population;

            return deathStatistic;
        }

        private static GenderFilter GetGender(string sex)
        {
            switch (sex)
            {
                case "T": return GenderFilter.All;
                case "M": return GenderFilter.Male;
                case "F": return GenderFilter.Female;
            }
            throw new ArgumentException("Unsuported gender");
        }

        public bool IsBuilt => DatabaseEngine.DoesTableExist(typeof(DeathStatistic));
    }
}
