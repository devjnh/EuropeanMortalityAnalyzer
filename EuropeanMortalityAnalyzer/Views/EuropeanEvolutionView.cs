using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MortalityAnalyzer.Views
{
    internal class EuropeanEvolutionView : MortalityEvolutionView
    {
        EuropeanImplementation EuropeanImplementation => (EuropeanImplementation)MortalityEvolution.Implementation;
        protected override void BuildAdditionalInfo(ExcelWorksheet workSheet)
        {
            DisplayCountries(workSheet, EuropeanImplementation.Countries);

        }

        public static void DisplayCountries(ExcelWorksheet workSheet, string[] countries)
        {
            if (countries.Length <= 1)
                return;
            int iRow = workSheet.Dimension.End.Row + 4;
            DisplayField(workSheet, iRow++, "Country", "Code");
            foreach (string country in countries)
                DisplayField(workSheet, iRow++, EuropeanImplementation.GetCountryDisplayName(country), country);
        }
    }
}
