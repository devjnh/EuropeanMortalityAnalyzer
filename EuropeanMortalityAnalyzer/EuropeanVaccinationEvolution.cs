using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    public class EuropeanVaccinationEvolution : VaccinationEvolution
    {
        public EuropeanVaccinationEvolution()
        {
            _Implementation = new EuropeanImplementation { MortalityEvolution = this };
        }
        public EuropeanImplementation EuropeanImplementation
        {
            get => (EuropeanImplementation)_Implementation;
        }
    }
}
