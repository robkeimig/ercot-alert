using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using HtmlAgilityPack;

namespace ErcotAlert
{
    class Program
    {
        const string LiveMarketPriceUrl = "http://www.ercot.com/content/cdr/html/current_np6788";
        const int CheckIntervalSeconds = 30;
        const string AlarmFile = @"alarm03.wav";

        static void Main(string[] args)
        {
            var httpClient = new HttpClient();

            Console.WriteLine("Enter price threshold in dollars:");
            var line = Console.ReadLine();
            var alertThreshold = decimal.Parse(line);

            while (true)
            {
                var httpResult = httpClient.GetAsync(LiveMarketPriceUrl).Result;
                var content = httpResult.Content.ReadAsStringAsync().Result;
                var pageDocument = new HtmlDocument();
                pageDocument.LoadHtml(content);
                var priceTable = pageDocument.DocumentNode.SelectSingleNode("//table/tbody");
                var prices = new List<decimal>();
                var parsedPriceCount = 0;

                foreach (var row in priceTable.SelectNodes("tr").Skip(2))
                {
                    var columns = row.SelectNodes("td");
                    var priceString = columns[3].InnerText.Trim();

                    if (decimal.TryParse(priceString, out decimal price))
                    {
                        prices.Add(price);
                        parsedPriceCount++;
                    }
                }

                var averagePricePerMegawattHour = prices.Sum() / parsedPriceCount;
                var averagePricePerKillowattHour = averagePricePerMegawattHour / 1000;
                Console.WriteLine($"{averagePricePerKillowattHour.ToString("C2")} per KWh");
                
                if (averagePricePerKillowattHour > alertThreshold)
                {
                    var process = Process.Start(@"powershell", $@"-c (New-Object Media.SoundPlayer '{AlarmFile}').PlaySync();");
                    process.WaitForExit();
                }
                
                Thread.Sleep(1000 * CheckIntervalSeconds);
            }
        }
    }
}
