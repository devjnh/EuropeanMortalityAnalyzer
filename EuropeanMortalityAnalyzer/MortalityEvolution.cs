﻿using CommandLine;
using MortalityAnalyzer.Parser;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    [Verb("evolution", HelpText = "French mortality evolution by years/semesters")]
    public class MortalityEvolution : Options
    {
        [Option("MinYearRegression", Required = false, HelpText = "2012 by default")]
        public int MinYearRegression { get; set; } = 2012;
        [Option("MaxYearRegression", Required = false, HelpText = "2020 by default")]
        public int MaxYearRegression { get; set; } = 2020;
        [Option("MinAge", Required = false, HelpText = "No lower age by default")]
        public int MinAge { get; set; } = -1;
        [Option("MaxAge", Required = false, HelpText = "No uper age limit by default")]
        public int MaxAge { get; set; } = -1;
        [Option('m', "TimeMode", Required = false, HelpText = "Time mode Year/Year+/Semester. Year by default")]
        public TimeMode TimeMode { get; set; } = TimeMode.Year;
        [Option('g', "Gender", Required = false, HelpText = "Gender mode All/Male/Female. All by default")]
        public GenderFilter GenderMode { get; set; } = GenderFilter.All;
        [Option('r', "Raw", Required = false, HelpText = "Display raw deaths in tables")]
        public bool DisplayRawDeaths { get; set; } = false;
        [Option('d', "Delay", Required = false, HelpText = "Delay in days between the last record and the max date used with the year to date mode")]
        public int ToDateDelay { get; set; } = 30;
        public string Country { get; set; }
        public string[] Countries { get; set; }
        public DateTime LastDay { get; private set; } = DateTime.MaxValue;
        internal DatabaseEngine DatabaseEngine { get; set; }
        public DataTable DataTable { get; private set; }
        public bool WholePeriods => TimeMode != TimeMode.YearToDate;
        public void Generate()
        {
            if (WholePeriods)
                LastDay = DateTime.MaxValue;
            else
                LastDay = Convert.ToDateTime(DatabaseEngine.GetValue($"SELECT MAX(Date) FROM {DeathStatistic.StatisticsTableName}")).AddDays(-ToDateDelay);

            string countryCondition = GetCountryCondition();
            DateTime firstDay = Convert.ToDateTime(DatabaseEngine.GetValue($"SELECT MAX(MinDate) FROM (SELECT MIN(DATE) AS MinDate FROM {DeathStatistic.StatisticsTableName} WHERE {countryCondition} GROUP BY Country)")).AddDays(-ToDateDelay);
            int minYear = firstDay.Year + 1;
            if (MinYearRegression > minYear)
                minYear = MinYearRegression;
            else
                MinYearRegression = minYear;

            string toDate = WholePeriods ? "" : " to date";
            Console.WriteLine($"Generating mortality evolution");
            StringBuilder conditionBuilder = new StringBuilder();
            if (!WholePeriods)
                AddCondition($"DayOfYear <= {LastDay.DayOfYear}", conditionBuilder);
            if (MinAge > 0)
                AddCondition($"Age >= {MinAge}", conditionBuilder);
            if (MaxAge > 0)
                AddCondition($"Age < {MaxAge}", conditionBuilder);
            //if (TimeMode == TimeMode.Semester)
            AddCondition($"Year >= {minYear}", conditionBuilder);
            string tablePostfix = string.Empty;
            if (GenderMode != GenderFilter.All)
                tablePostfix = $"_{GenderMode}";
            if (!string.IsNullOrWhiteSpace(countryCondition))
                AddCondition(countryCondition, conditionBuilder);
            else
                AddCondition($"Country NOT IN( 'AD', 'GE', 'UK', 'AL', 'AM') ", conditionBuilder);
            string query = string.Format(Query_Years, conditionBuilder.Length > 0 ? $" WHERE {conditionBuilder}" : "", GetTimeGroupingField(TimeMode), tablePostfix);
            DataTable = DatabaseEngine.GetDataTable(query);
            if (TimeMode == TimeMode.DeltaYear)
                DataTable.Rows.Remove(DataTable.Rows[0]);
            if (WholePeriods)
                DataTable.Rows.Remove(DataTable.Rows[DataTable.Rows.Count - 1]);
            if (WholePeriods)
                foreach (DataRow dataRow in DataTable.Rows)
                {
                    double days = GetPeriodLength(Convert.ToDateTime(dataRow[3]), Convert.ToDateTime(dataRow[4]).AddDays(7));
                    dataRow[1] = Convert.ToDouble(dataRow[1]) * StandardizedPeriodLength / days; // Standardize according to period length
                }
            DataTable.Columns.Remove("MaxDate");
            DataTable.Columns.Remove("MinDate");
            BuildLinearRegression(DataTable, MinYearRegression, MaxYearRegression);
            BuildExcessHistogram();
        }



        public double StandardizedPeriodLength => 365 * PeriodInFractionOfYear;

        private double PeriodInFractionOfYear
        {
            get
            {
                return TimeMode switch
                {
                    TimeMode.Semester => 0.5,
                    TimeMode.Quarter => 0.25,
                    _ => 1.0,
                };
            }
        }

        private int PeriodInMonths
        {
            get
            {
                return TimeMode switch
                {
                    TimeMode.Semester => 6,
                    TimeMode.Quarter => 3,
                    _ => 12
                };
            }
        }

        private double GetPeriodLength(double period)
        {
            int year = (int)period;
            int month = TimeMode == TimeMode.DeltaYear ? 7 : (int)((period - year) * 12) + 1;
            DateTime periodStart = new DateTime(year, month, 1);
            DateTime periodEnd = periodStart.AddMonths(PeriodInMonths);
            double days = (periodEnd - periodStart).TotalDays;
            return days;
        }
        private double GetPeriodLength(DateTime periodStart, DateTime periodEnd)
        {

            double days = (periodEnd - periodStart).TotalDays;
            return days;
        }

        private void AddCondition(string condition, StringBuilder conditionsBuilder)
        {
            if (conditionsBuilder.Length > 0)
                conditionsBuilder.Append(" AND ");
            conditionsBuilder.Append(condition);
        }
        private const string Query_Years = @"SELECT {1}, SUM(DeathStatistics{2}.StandardizedDeaths) AS Standardized, SUM(DeathStatistics{2}.Deaths) AS Raw, MIN(Date) AS MinDate, MAX(Date) AS MaxDate FROM DeathStatistics{2}{0}
GROUP BY {1}
ORDER BY {1}";

        string GetTimeGroupingField(TimeMode mode)
        {
            switch (mode)
            {
                case TimeMode.DeltaYear: return nameof(DeathStatistic.DeltaYear);
                case TimeMode.Semester: return nameof(DeathStatistic.Semester);
                case TimeMode.Quarter: return nameof(DeathStatistic.Quarter);
                default: return nameof(DeathStatistic.Year);
            }
        }
        private void BuildLinearRegression(DataTable dataTable, int minYearRegression, int maxYearRegression)
        {
            List<double> xVals = new List<double>();
            List<double> yVals = new List<double>();
            foreach (DataRow dataRow in dataTable.Rows)
            {
                double year = Convert.ToDouble(dataRow[0]);
                if (year < minYearRegression || year >= maxYearRegression)
                    continue;
                xVals.Add(year);
                yVals.Add(Convert.ToDouble(dataRow[1]));
            }
            double rsquared, yintercept, slope;
            LinearRegression(xVals, yVals, 0, xVals.Count, out rsquared, out yintercept, out slope);
            dataTable.Columns.Add("BaseLine", typeof(double));
            dataTable.Columns.Add("Excess", typeof(double));
            dataTable.Columns.Add("RelativeExcess", typeof(double));
            foreach (DataRow dataRow in dataTable.Rows)
            {
                double year = Convert.ToDouble(dataRow[0]);
                double baseLine = yintercept + slope * year;
                dataRow["BaseLine"] = baseLine;
                double excess = Convert.ToDouble(dataRow[1]) - baseLine;
                dataRow["Excess"] = excess;
                dataRow["RelativeExcess"] = excess / baseLine;
            }
        }
        /// <summary>
        /// Fits a line to a collection of (x,y) points.
        /// </summary>
        /// <param name="xVals">The x-axis values.</param>
        /// <param name="yVals">The y-axis values.</param>
        /// <param name="inclusiveStart">The inclusive inclusiveStart index.</param>
        /// <param name="exclusiveEnd">The exclusive exclusiveEnd index.</param>
        /// <param name="rsquared">The r^2 value of the line.</param>
        /// <param name="yintercept">The y-intercept value of the line (i.e. y = ax + b, yintercept is b).</param>
        /// <param name="slope">The slop of the line (i.e. y = ax + b, slope is a).</param>
        public static void LinearRegression(IList<double> xVals, IList<double> yVals,
                                            int inclusiveStart, int exclusiveEnd,
                                            out double rsquared, out double yintercept,
                                            out double slope)
        {
            double sumOfX = 0;
            double sumOfY = 0;
            double sumOfXSq = 0;
            double sumOfYSq = 0;
            double ssX = 0;
            double ssY = 0;
            double sumCodeviates = 0;
            double sCo = 0;
            double count = exclusiveEnd - inclusiveStart;

            for (int ctr = inclusiveStart; ctr < exclusiveEnd; ctr++)
            {
                double x = xVals[ctr];
                double y = yVals[ctr];
                sumCodeviates += x * y;
                sumOfX += x;
                sumOfY += y;
                sumOfXSq += x * x;
                sumOfYSq += y * y;
            }
            ssX = sumOfXSq - ((sumOfX * sumOfX) / count);
            ssY = sumOfYSq - ((sumOfY * sumOfY) / count);
            double RNumerator = (count * sumCodeviates) - (sumOfX * sumOfY);
            double RDenom = (count * sumOfXSq - (sumOfX * sumOfX))
             * (count * sumOfYSq - (sumOfY * sumOfY));
            sCo = sumCodeviates - ((sumOfX * sumOfY) / count);

            double meanX = sumOfX / count;
            double meanY = sumOfY / count;
            double dblR = RNumerator / Math.Sqrt(RDenom);
            rsquared = dblR * dblR;
            yintercept = meanY - ((sCo / ssX) * meanX);
            slope = sCo / ssX;
        }

        public static double GetHistogramResolution(double rangeLength, int approximateBarCount, bool allowLessThanOne = false)
        {
            if (rangeLength == 0.0)
                return 1.0;
            double histogramResolution = rangeLength / approximateBarCount;
            if (!allowLessThanOne && histogramResolution < 1.0)
                return 1.0; //  Less than 1 range length not supported for now
            double logResolution = Math.Log10(histogramResolution);
            double log10 = Math.Floor(logResolution);
            double restLogResolution = logResolution - log10;
            double baseResolution = Math.Pow(10, log10);
            if (restLogResolution < Math.Log10(2))
                histogramResolution = baseResolution;
            else if (restLogResolution < Math.Log10(5))
                histogramResolution = baseResolution * 2;
            else
                histogramResolution = baseResolution * 5;
            return histogramResolution;
        }
        public DataTable ExcessHistogram { get; private set; }
        public double StandardDeviation { get; private set; }
        public int Population
        {
            get
            {
                string countryCondition = GetCountryCondition();
                string sqlCommand = $"SELECT SUM(Population) FROM AgeStructure WHERE Year = {EuroStatWeekly.ReferenceYear} AND {countryCondition} AND Gender = {(int)GenderMode}";
                if (MinAge >= 0)
                    sqlCommand += $" AND Age >= {MinAge}";
                if (MaxAge >= 0)
                    sqlCommand += $" AND Age < {MaxAge}";
                return Convert.ToInt32(DatabaseEngine.GetValue(sqlCommand));
            }
        }

        private string GetCountryCondition()
        {
            string countryCondition = $"Country NOT IN( 'AD', 'GE', 'UK', 'AL', 'AM') "; ;
            if (!string.IsNullOrEmpty(Country))
                countryCondition = $"Country = '{Country}'";
            else if (Countries != null && Countries.Length > 0)
            {
                string countrySqlList = String.Join(",", Countries.Select(c => String.Format("'{0}'", c)));
                countryCondition = $"Country IN ({countrySqlList})";
            }
            return countryCondition;
        }

        public string GetCountryDisplayName()
        {
            if (!string.IsNullOrEmpty(Country))
                return Country;
            else if (Countries != null && Countries.Length > 0 && Countries.Length < 5)
                return string.Join(" ", Countries);
            else
                return string.Empty;
        }
        public string GetCountryInternalName()
        {
            if (!string.IsNullOrEmpty(Country))
                return Country;
            else if (Countries != null && Countries.Length > 0)
                return Countries.Length < 5 ? string.Join("", Countries) : "Multi";
            else
                return string.Empty;
        }

        double DeathRate { get; set; }

        void BuildExcessHistogram()
        {
            var values = DataTable.AsEnumerable().Where(r => Convert.ToDouble(r.Field<object>(GetTimeGroupingField(TimeMode))) > MinYearRegression).Select(r => r.Field<double>("Excess")).ToArray();
            double max = values.Max();
            double min = values.Min();
            double resolution = GetHistogramResolution(max - min, 15);
            double upperBound = (Math.Floor(max / resolution) * resolution);
            double lowerBound = (Math.Floor(min / resolution) * resolution);
            int bars = (int)((upperBound - lowerBound) / resolution) + 1;
            int[] frequencies = new int[bars];
            foreach (double item in values)
            {
                int iBar = (int)((item - lowerBound) / resolution);
                frequencies[iBar]++;
            }
            ExcessHistogram = new DataTable();
            ExcessHistogram.Columns.Add("Excess", typeof(double));
            ExcessHistogram.Columns.Add("Frequency", typeof(double));
            ExcessHistogram.Columns.Add("Normal", typeof(double));
            double[] standardizedDeaths = DataTable.AsEnumerable().Where(r => Convert.ToDouble(r.Field<object>(GetTimeGroupingField(TimeMode))) > MinYearRegression).Select(r => r.Field<double>("Standardized")).ToArray();
            double averageDeaths = standardizedDeaths.Average();
            DeathRate = averageDeaths / Population;
            StandardDeviation = Math.Sqrt(DeathRate * (1 - DeathRate) * Population);
            double sum = standardizedDeaths.Sum();
            for (int i = 0; i < frequencies.Length; i++)
            {
                DataRow dataRow = ExcessHistogram.NewRow();
                double x = lowerBound + i * resolution;
                dataRow["Excess"] = x;
                dataRow["Frequency"] = frequencies[i];

                double y = Math.Exp(-0.5 * Math.Pow(x / StandardDeviation, 2)) / (Math.Sqrt(2 * Math.PI) * StandardDeviation) * resolution * values.Count();
                dataRow["Normal"] = y;
                ExcessHistogram.Rows.Add(dataRow);
            }
        }
    }
    public enum TimeMode { Year, DeltaYear, Semester, Quarter, YearToDate }
}
