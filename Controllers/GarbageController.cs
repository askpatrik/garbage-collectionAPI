using garbage_collectionAPI.Data;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace garbage_collectionAPI.Controllers
{
    [ApiController]
    [Route("api/garbage")]
    public class GarbageController : Controller
    {
        private readonly HttpClient _httpClient;

        public GarbageController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet ("b")]
        public async Task<IActionResult> GetGarbageInfo()
        {
            var response = await _httpClient.GetAsync("\r\nhttps://webbservice.indecta.se/kunder/sjobo/kalender/basfiler/onlinekalender.php?hsG=Norra+Eggelstadsv%E4gen+57&hsO=L%F6vestad");
            if (response.IsSuccessStatusCode)
            {
                var html = await response.Content.ReadAsStringAsync();
                var htmlDoc = new HtmlDocument();

                htmlDoc.LoadHtml(html);

                // Find rows that contain styleMonthName
                var rows = htmlDoc.DocumentNode.SelectNodes("//tr[td[contains(@class, 'styleMonthName')] or div[contains(@class, 'styleInteIdag')]]");


                var result = rows.Select(row => row.OuterHtml).ToList();

                var data = await response.Content.ReadAsStringAsync();
                return Ok(result); // Returning data as a JSON string
            }
            else
            {
                return StatusCode((int)response.StatusCode, "Error fetching data");
            }
           

            // <td width="80%" colspan="5" class="styleMonthName">December - 2025</td>
            // <div class="styleInteIdag">26</div>
            //<span class="dagMedTomClass1">1</span></td>
     

        }
        [HttpGet]
        public async Task<IActionResult> GetMonthlyGarbage()
        {
            List<DayData> datesAndKärls = new List<DayData>();  
            var response = await _httpClient.GetAsync("\r\nhttps://webbservice.indecta.se/kunder/sjobo/kalender/basfiler/onlinekalender.php?hsG=Norra+Eggelstadsv%E4gen+57&hsO=L%F6vestad");
            if (response.IsSuccessStatusCode)
            {
                string html = await response.Content.ReadAsStringAsync();
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                // Select rows for February - 2025
                var rows = htmlDoc.DocumentNode.SelectNodes("//tr[td[contains(@class, 'styleMonthName') and contains(text(), 'Februari - 2025')]]");

                if (rows != null)
                {
                    var allRows = new List<string>();

                    // Add February rows
                    allRows.AddRange(rows.Select(row => row.OuterHtml));

                    // Extract rows until Mars - 2025
                    var nextRow = rows.Last().NextSibling;
                    while (nextRow != null && !nextRow.InnerHtml.Contains("styleMonthName") && !nextRow.InnerHtml.Contains("Mars - 2025"))
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
                            Karl = kärlNumber
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


        // GET: GarbageController
        public ActionResult Index()
        {
            return View();
        }


        // POST: GarbageController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: GarbageController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: GarbageController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: GarbageController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: GarbageController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
