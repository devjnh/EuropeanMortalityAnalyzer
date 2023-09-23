using MortalityAnalyzer.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    public class EuropeanMortalityEvolution : MortalityEvolution
    {
        protected const string Query_Years = @"SELECT {1}, SUM(DeathStatistics{2}.StandardizedDeaths) AS Standardized, SUM(DeathStatistics{2}.Deaths) AS Raw, MIN(Date) AS MinDate, MAX(Date) AS MaxDate FROM DeathStatistics{2}{0}
GROUP BY {1}
ORDER BY {1}";

        protected override string GetQueryTemplate()
        {
            return Query_Years;
        }

        protected override void CleanDataTable()
        {
            DataTable.Columns.Remove("MaxDate");
            DataTable.Columns.Remove("MinDate");
        }

        protected override void AdjustMinYearRegression(string countryCondition)
        {
            DateTime firstDay = Convert.ToDateTime(DatabaseEngine.GetValue($"SELECT MAX(MinDate) FROM (SELECT MIN(DATE) AS MinDate FROM {DeathStatistic.StatisticsTableName} WHERE {countryCondition} GROUP BY Country)")).AddDays(-ToDateDelay);
            int minYear = firstDay.Year + 1;
            if (MinYearRegression > minYear)
                minYear = MinYearRegression;
            else
                MinYearRegression = minYear;
        }

        protected override double GetPeriodLength(DataRow dataRow)
        {
            return GetPeriodLength(Convert.ToDateTime(dataRow[3]), Convert.ToDateTime(dataRow[4]).AddDays(7));
        }

        protected override string GetPopulationSqlQuery()
        {
            return $"SELECT SUM(Population) FROM AgeStructure WHERE Year = {AgeStructure.ReferenceYear} AND Gender = {(int)GenderMode}";
        }

        public string Country { get; set; }
        public string[] Countries { get; set; }

        protected override string GetCountryCondition()
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

        public override string GetCountryDisplayName()
        {
            if (!string.IsNullOrEmpty(Country))
                return Country;
            else if (Countries != null && Countries.Length > 0 && Countries.Length < 5)
                return string.Join(" ", Countries);
            else
                return string.Empty;
        }
        public override string GetCountryInternalName()
        {
            if (!string.IsNullOrEmpty(Country))
                return Country;
            else if (Countries != null && Countries.Length > 0)
                return Countries.Length < 5 ? string.Join("", Countries) : "Multi";
            else
                return string.Empty;
        }
    }
}
