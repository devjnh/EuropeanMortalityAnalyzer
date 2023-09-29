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
        public EuropeanMortalityEvolution()
        {
            _Implementation = new EuropeanImplementation { MortalityEvolution = this};
        }
        public EuropeanImplementation EuropeanImplementation
        {
            get => (EuropeanImplementation)_Implementation;
        }
                
    }
}
