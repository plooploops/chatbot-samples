using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using System.Linq;

namespace html_json_fa_test
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string html = data.body;
            string jsonLinesContent = TestHTML(html);

            return jsonLinesContent != null
                ? (ActionResult)new OkObjectResult($"{jsonLinesContent}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        public static string TestHTML(string html, int skipCount = 0)
        {
            string ret = String.Empty;
            StringBuilder sb = new StringBuilder(); //placeholder for JSON Lines

            JObject t = new JObject();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            //first pass looking at table.
            var tables = doc.DocumentNode.SelectNodes("//table/tbody").Skip(skipCount);
            int tCount = 0;
            foreach (var table in tables)
            {
                t.Add("id", tCount++);
                //in each table, check the children
                var rowCells = table.SelectNodes("tr/td");

                //first row
                if (rowCells.Count > 0)
                {
                    var appNameNode = rowCells[0].SelectSingleNode("span/strong/span");
                    if (appNameNode == null)
                    {
                        //corner case <p><span><strong>app name</strong></span></p>
                        appNameNode = rowCells[0].SelectSingleNode("p/span/strong");
                    }
                    //another fallback case
                    if (appNameNode == null)
                    {
                        appNameNode = rowCells[0].SelectSingleNode("span/strong");
                    }

                    //fix html formatting
                    t.Add("appName", new JValue(appNameNode.ChildNodes[0].GetDirectInnerText().Replace(" ", "").Replace("\r\n", " ").Replace("&nbsp;", "").Trim()));
                    t.Add("appType", new JValue(string.Join(' ', appNameNode.ChildNodes[1].GetDirectInnerText().Split("\r\n").Select(_ => _.Trim())).Trim()));

                    //skip rest of header rows
                    int filteredRowIndex = 1;
                    for (int rSkip = 1; rSkip < rowCells.Count; rSkip++)
                    {
                        if (rowCells[rSkip].Attributes["colspan"] == null)
                            break;
                        filteredRowIndex++;
                    }

                    //look at key value pairs
                    string key = String.Empty;
                    string value = string.Empty;
                    HtmlNode curNode;
                    HtmlNode childNode;

                    bool parsingKey = true; // more cases with multiple cells in a row.  Can switch later.

                    for (int r = filteredRowIndex; r < rowCells.Count; r++)
                    {
                        curNode = rowCells[r];

                        if (parsingKey)
                        {
                            childNode = curNode.SelectSingleNode("span/strong");
                            if (childNode == null)
                            {
                                //corner case <p><span><strong>key</strong></span><p>
                                childNode = curNode.SelectSingleNode("p/span/strong");
                            }
                            //format key
                            key = string.Join(' ', childNode.InnerText.Split("\r\n").Select(_ => _.Trim())).Replace("\n","").Trim().TrimEnd(':');
                        }
                        else
                        {
                            childNode = curNode.SelectSingleNode("span");
                            if (childNode == null)
                            {
                                //corner case <p><span>stuff</span></p>
                                childNode = curNode.SelectSingleNode("p");
                            }
                            //format value
                            if (childNode.InnerHtml.Contains("\r\n"))
                            {
                                value = string.Join(' ', childNode.InnerHtml.Split("\r\n").Select(_ => _.Trim())).Trim();
                            }
                            else
                            {
                                value = childNode.InnerHtml;
                            }

                            value = value.Replace("\n", "").Trim();
                        }

                        if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(key))
                        {
                            t.Add(key, new JValue(value));
                            key = value = string.Empty;
                        }

                        parsingKey = !parsingKey;
                    }
                }

                sb.AppendLine(t.ToString());

                t = new JObject();
            }

            ret = sb.ToString();

            return ret;
        }
    }
}
