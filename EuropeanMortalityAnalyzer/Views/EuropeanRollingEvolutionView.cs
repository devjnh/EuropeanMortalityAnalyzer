using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Views
{
    internal class EuropeanRollingEvolutionView : RollingEvolutionView
    {
        EuropeanImplementation EuropeanImplementation => (EuropeanImplementation)MortalityEvolution.Implementation;
        protected override void BuildAdditionalInfo(ExcelWorksheet workSheet)
        {
            EuropeanEvolutionView.DisplayCountries(workSheet, EuropeanImplementation.Countries);
        }
    }
}
