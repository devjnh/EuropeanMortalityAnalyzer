using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EuropeanMortalityAnalyzer.Downloaders
{
    internal class EuroStatDownloader
    {
        public EuroStatDownloader(string logFolder)
        {
            LogFolder = logFolder;
        }
        public string LogFolder { get; }
        public void Download(string fileName)
        {
            string url = $"https://ec.europa.eu/eurostat/api/dissemination/sdmx/2.1/data/{fileName.ToUpper()}/?format=SDMX-CSV&compressed=true&i";
            Console.WriteLine($"Downloading file {fileName}.csv {url}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            using (HttpWebResponse reponse = (HttpWebResponse)request.GetResponse())
            {
                using (GZipStream zipStream = new GZipStream(reponse.GetResponseStream(), CompressionMode.Decompress))
                {
                    using (FileStream fOutStream = new FileStream(Path.Combine(LogFolder, $"{fileName}.csv"), FileMode.Create, FileAccess.Write))
                    {
                        zipStream.CopyTo(fOutStream);
                    }
                }
            }
        }
    }
}
