using CommandLine;
using MortalityAnalyzer.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    [Verb("init", HelpText = "Parse and insert data in the database")]
    public class InitOptions : Options
    {
    }
    [Verb("show", HelpText = "Display the Excel spreadsheet")]
    public class ShowOptions : Options
    {
    }
    [Verb("evolution", HelpText = "European mortality evolution")]
    public class MortalityEvolutionOptions : MortalityEvolutionBase
    {
        [Option("Countries", Required = false, HelpText = "List of european countries to include.")]
        public IEnumerable<string> Countries { get; set; }
        [Option("Area", Required = false, HelpText = "Display name of the area.")]
        public string Area { get; set; }

        public MortalityEvolution GetEvolutionEngine()
        {
            MortalityEvolution mortalityEvolution = IsNormalMode ? new EuropeanMortalityEvolution() : new EuropeanRollingEvolution();
            CopyTo(mortalityEvolution);
            EuropeanImplementation europeanImplementation = ((EuropeanImplementation)mortalityEvolution.Implementation);
            europeanImplementation.Countries = Countries.ToArray();
            europeanImplementation.Area = Area;

            return mortalityEvolution;
        }

        private bool IsNormalMode => TimeMode <= TimeMode.Month;

        internal BaseEvolutionView GetView()
        {
            return IsNormalMode ? new MortalityEvolutionView() : new RollingEvolutionView();
        }
    }
}
