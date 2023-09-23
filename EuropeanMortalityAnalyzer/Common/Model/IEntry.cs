using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    internal interface IEntry
    {
        void ToRow(DataRow dataRow);
    }
}
