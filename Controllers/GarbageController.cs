using garbage_collectionAPI.Data;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Linq;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using SimpleFeedReader;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

namespace garbage_collectionAPI.Controllers
{
    [ApiController]
    [Route("api/")]

    // TODO: Create model and add name of the place of military training. 
    public class GarbageController : Controller
    {
        private readonly HttpClient _httpClient;
        public static DateTime Month = DateTime.Now;
        public DateTime NextMonth = Month.AddMonths(1);

        public GarbageController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet("military/")]
        public async Task<IActionResult> GetMilitaryTrainings()
        {
            Dictionary<string, string> avlysningar = new Dictionary<string, string>();
            List<string> datesAndTimes = new List<string>();

            var reader1 = new FeedReader();
            var items = reader1.RetrieveFeed("https://www.forsvarsmakten.se/sv/aktuellt/viktiga-meddelanden/skjutfalt-och-avlysningar/bjorka-ovningsfalt/feed.rss");

            foreach (var i in items)
            {
                if (i.Title.StartsWith("a"))
                    avlysningar.Add(i.Title, null);
            }

            foreach (var item in avlysningar.Keys)
            {
                // URL to the PDF
                string pdfUrl = $"https://www.forsvarsmakten.se/siteassets/skjutfalt-avlysningar/bjorka/{item}";

                try
                {
                    // Download the PDF from the URL
                    using (WebClient webClient = new WebClient())
                    {
                        byte[] pdfData = webClient.DownloadData(pdfUrl);

                        // Open the PDF
                        using (PdfReader reader = new PdfReader(pdfData))
                        {
                            List<string> allPageTexts = new List<string>();

                            for (int pageNum = 1; pageNum <= reader.NumberOfPages; pageNum++)
                            {
                                // Extract text from each page using PdfTextExtractor
                                string pageText = PdfTextExtractor.GetTextFromPage(reader, pageNum);
                                allPageTexts.Add(pageText);
                            }

                            // Combine all page texts into a single string for the specific item
                            avlysningar[item] = string.Join("\n", allPageTexts);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }

            foreach (var item in avlysningar)
            {
                // Regex pattern to match lines with dates and times
                string pattern = @"(\d{4}-\d{2}-\d{2}) (\d{4}-\d{4})";
                var matches = Regex.Matches(item.Value, pattern);

                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        if (match.Success)
                        {
                            string date = match.Groups[1].Value;
                            string time = match.Groups[2].Value;
                            datesAndTimes.Add($"Datum: {date} Tid: {time}");
                        }
                    }
                }
                else
                {
                    // Optionally handle case where no dates and times are found
                    datesAndTimes.Add("No dates and times found.");
                }
            }

            return Ok(datesAndTimes);
        }

        [HttpGet("garbage")]
        public async Task<IActionResult> GetMonthlyGarbage()
        {
            // month now
            var month = Month.ToString("MMMM", new CultureInfo("sv-SE"));
            var monthRight = char.ToUpper(month[0]) + month.Substring(1).ToLower();

            // next month
            var nextMonth = Month.ToString("MMMM", new CultureInfo("sv-SE"));
            var nextMonthRight = char.ToUpper(month[0]) + month.Substring(1).ToLower();

            List<DayData> datesAndKärls = new List<DayData>();
            var response = await _httpClient.GetAsync("https://webbservice.indecta.se/kunder/sjobo/kalender/basfiler/onlinekalender.php?hsG=Norra+Eggelstadsv%E4gen+57&hsO=L%F6vestad");

            if (response.IsSuccessStatusCode)
            {
                string html = await response.Content.ReadAsStringAsync();
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                // Select rows for the current month
                var rows = htmlDoc.DocumentNode.SelectNodes($"//tr[td[contains(@class, 'styleMonthName') and contains(text(), '{monthRight} - 2024')]]");

                if (rows != null)
                {
                    var allRows = new List<string>();

                    // Add current month rows
                    allRows.AddRange(rows.Select(row => row.OuterHtml));

                    // Extract rows until next month
                    var nextRow = rows.Last().NextSibling;
                    while (nextRow != null && !nextRow.InnerHtml.Contains("styleMonthName") && !nextRow.InnerHtml.Contains($"{nextMonthRight} - 2025"))
                    {
                        allRows.Add(nextRow.OuterHtml);
                        nextRow = nextRow.NextSibling;
                    }

                    var newRows = allRows.Where(x => x.Contains("dagMedTomClass"));

                    foreach (var item in newRows)
                    {
                        var indexOfDate = item.IndexOf("Idag\"");
                        var date = item.Substring(indexOfDate + 6, 2);

                        var indexOfKärl = item.IndexOf("TomClass");
                        var kärlNumber = item.Substring(indexOfKärl + 11, 1);

                        DayData dayData = new DayData()
                        {
                            Day = date,
                            Container = kärlNumber,
                            Month = monthRight
                        };
                        datesAndKärls.Add(dayData);
                    }

                    return Ok(datesAndKärls); // Returning list of rows as a JSON array
                }
                else
                {
                    return Ok(new List<string>()); // Returning empty list if no rows are found
                }
            }
            else
            {
                return StatusCode((int)response.StatusCode, "Error fetching data");
            }
        }
    }
}
