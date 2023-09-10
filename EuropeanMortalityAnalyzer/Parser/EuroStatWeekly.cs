using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EuropeanMortalityAnalyzer.Parser
{
    internal class EuroStatWeekly
    {
        public DatabaseEngine DatabaseEngine { get; set; }
        public AgeStructure AgeStructure { get; set; }
        public bool FilesInserted { get; set; } = false;
        static public int ReferenceYear { get; set; } = 2022;
        public void Extract(string deathsLogFolder)
        {
            DatabaseEngine.Prepare(DeathStatistic.CreateDataTable(GenderFilter.All), false);
            using (FileStream fileStream = new FileStream(Path.Combine(deathsLogFolder, "demo_r_mwk_05_linear.csv"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader textReader = new StreamReader(fileStream))
                {
                    string line = textReader.ReadLine();
                    string[] header = line.Split(',');
                    while (!textReader.EndOfStream)
                    {
                        line = textReader.ReadLine();
                        string[] split = line.Split(',');
                        string age = split[3];
                        string sex = split[4];
                        string geo = split[6];
                        string period = split[7];
                        string count = split[8];
                        if (sex != "T" || age == "TOTAL" || age == "UNK")
                            continue;
                        Regex regexWeek = new Regex("([0-9]{4})-W([0-9]{2})");
                        var result = regexWeek.Match(period);
                        if (!result.Success)
                            continue;
                        int year = Convert.ToInt32(result.Groups[1].Value);
                        int week = Convert.ToInt32(result.Groups[2].Value);
                        DateTime weekDate = FirstDateOfWeekISO8601(year, week);
                        Regex regexAge = new Regex("Y([0-9]+)-([0-9]+)");
                        result = regexAge.Match(age);
                        if (!result.Success)
                            continue;
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
                        deathStatistic.RefPopulation = AgeStructure.GetPopulation(ReferenceYear, minAge, maxAge + 1, geo);
                        deathStatistic.StandardizedDeaths = (double)deathStatistic.Deaths * deathStatistic.RefPopulation / deathStatistic.Population;
                        DatabaseEngine.Insert(deathStatistic);
                    }
                }
            }
            DatabaseEngine.FinishInsertion();
        }
        public static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            // Use first Thursday in January to get first week of the year as
            // it will never be in Week 52/53
            DateTime firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            // As we're adding days to a date in Week 1,
            // we need to subtract 1 in order to get the right date for week #1
            if (firstWeek == 1)
            {
                weekNum -= 1;
            }

            // Using the first Thursday as starting week ensures that we are starting in the right year
            // then we add number of weeks multiplied with days
            var result = firstThursday.AddDays(weekNum * 7);

            // Subtract 3 days from Thursday to get Monday, which is the first weekday in ISO8601
            return result.AddDays(-3);
        }
        public bool IsBuilt => DatabaseEngine.DoesTableExist(DeathStatistic.StatisticsTableName);
    }
}
