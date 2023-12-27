using MortalityAnalyzer.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MortalityAnalyzer
{
    public class EuropeanMortalityHelper : EuropeanMortalityEvolution
    {
        public EuropeanMortalityHelper(MortalityEvolution mortalityEvolution)
        {
            DatabaseEngine = mortalityEvolution.DatabaseEngine;
            mortalityEvolution.CopyTo(this);
        }
        protected override string GetCountryCondition()
        {
            return string.Empty;
        }
        public IEnumerable<string> GetSupportedCountries()
        {
            StringBuilder conditionBuilder = new StringBuilder();
            AddConditions(conditionBuilder);
            DataTable allCountries = DatabaseEngine.GetDataTable($"SELECT Country, MAX(Year) AS MaxYear FROM DeathStatistics{conditionBuilder} GROUP BY Country HAVING MaxYear >= {AgeStructure.ReferenceYear}");
            foreach (string country in allCountries.AsEnumerable().Select(c => c.Field<string>("Country")))
            {
                try
                {
                    EuropeanMortalityEvolution europeanMortalityEvolution = new EuropeanMortalityEvolution { DatabaseEngine = DatabaseEngine };
                    CopyTo(europeanMortalityEvolution);
                    europeanMortalityEvolution.EuropeanImplementation.Country = country;
                    int population = europeanMortalityEvolution.Population;
                }
                catch (InvalidCastException) { continue; }
                yield return country;
            }
        }
    }
}
