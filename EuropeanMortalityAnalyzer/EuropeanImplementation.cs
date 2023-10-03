using MortalityAnalyzer.Common;
using MortalityAnalyzer.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    public class EuropeanImplementation : SpecificImplementation
    {
        protected const string Query_Years = @"SELECT {1}, SUM(DeathStatistics{2}.StandardizedDeaths) AS Standardized, SUM(DeathStatistics{2}.Deaths) AS Raw, MIN(Date) AS MinDate, MAX(Date) AS MaxDate FROM DeathStatistics{2}{0}
GROUP BY {1}
ORDER BY {1}";

        public override string GetQueryTemplate()
        {
            return Query_Years;
        }

        public override void CleanDataTable(DataTable dataTable)
        {
            dataTable.Columns.Remove("MaxDate");
            dataTable.Columns.Remove("MinDate");
        }

        public override void AdjustMinYearRegression(string countryCondition)
        {
            DateTime firstDay = Convert.ToDateTime(MortalityEvolution.DatabaseEngine.GetValue($"SELECT MAX(MinDate) FROM (SELECT MIN(DATE) AS MinDate FROM {DeathStatistic.StatisticsTableName} WHERE {countryCondition} GROUP BY Country)")).AddDays(-MortalityEvolution.ToDateDelay);
            int minYear = firstDay.Year + 1;
            if (MortalityEvolution.MinYearRegression > minYear)
                minYear = MortalityEvolution.MinYearRegression;
            else
                MortalityEvolution.MinYearRegression = minYear;
        }

        public override double GetPeriodLength(DataRow dataRow)
        {
            return MortalityEvolution.GetPeriodLength(Convert.ToDateTime(dataRow[3]), Convert.ToDateTime(dataRow[4]).AddDays(7));
        }

        public override string GetPopulationSqlQuery()
        {
            return $"SELECT SUM(Population) FROM AgeStructure WHERE Year = {AgeStructure.ReferenceYear} AND Gender = {(int)MortalityEvolution.GenderMode}";
        }

        public string Country { get; set; }
        public string[] Countries { get; set; }

        public override string GetCountryCondition()
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
                return new RegionInfo(Country).DisplayName;
            else if (Countries != null && Countries.Length > 0 && Countries.Length < 5)
                return string.Join(" ", Countries.Select(c => new RegionInfo(c).DisplayName));
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
