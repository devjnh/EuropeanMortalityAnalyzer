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
    [Verb("allcountries", HelpText = "Mortality evolution for countries listed in EuroStat")]
    public class AllCountriesEvolutionOptions : MortalityEvolutionBase
    {
    }
    [Verb("countries", HelpText = "Mortality evolution for several countries")]
    public class CountriesEvolutionOptions : MortalityEvolutionBase
    {
        [Option("Countries", Required = false, HelpText = "List of european countries to include.")]
        public IEnumerable<string> Countries { get; set; }
        [Option("Area", Required = false, HelpText = "Display name of the area.")]
        public string Area { get; set; }
    }
    [Verb("area", HelpText = "Mortality evolution for a group of countries")]
    public class MortalityEvolutionOptions : CountriesEvolutionOptions
    {
        public MortalityEvolution GetEvolutionEngine(TimeMode timeMode, int rollingPeriod)
        {
            MortalityEvolution mortalityEvolution = timeMode <= TimeMode.Month ? new EuropeanMortalityEvolution() : new EuropeanRollingEvolution();
            CopyTo(mortalityEvolution);
            mortalityEvolution.TimeMode = timeMode;
            mortalityEvolution.RollingPeriod = rollingPeriod;
            EuropeanImplementation europeanImplementation = ((EuropeanImplementation)mortalityEvolution.Implementation);
            europeanImplementation.Countries = Countries.ToArray();
            europeanImplementation.Area = Area;

            return mortalityEvolution;
        }
    }

}
