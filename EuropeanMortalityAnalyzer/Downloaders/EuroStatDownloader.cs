using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Downloaders
{
    internal class EuroStatDownloader : FileDownloader
    {
        public EuroStatDownloader(string logFolder) : base(logFolder)
        {
        }
        public void Download(string fileName)
        {
            string url = $"https://ec.europa.eu/eurostat/api/dissemination/sdmx/2.1/data/{fileName.ToUpper()}/?format=SDMX-CSV&compressed=true&i";
            Download($"{fileName}.csv", url);
        }


        override protected void WriteToFile(string fileName, Stream responseStream)
        {
            using (GZipStream zipStream = new GZipStream(responseStream, CompressionMode.Decompress))
            {
                base.WriteToFile(fileName, zipStream);
            }
        }

    }
}
