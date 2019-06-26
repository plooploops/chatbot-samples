using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoBot1.Helper
{
    public static class HTMLHelper
    {
        public static string TestHTML(int skipCount = 0)
        {
            string ret = String.Empty;
            StringBuilder sb = new StringBuilder(); //placeholder for JSON Lines

            JObject t = new JObject();

            string filePath = @".\Helper\test-page.html";

            var doc = new HtmlDocument();
            doc.Load(filePath);

            //first pass looking at table.
            var tables = doc.DocumentNode.SelectNodes("//table/tbody").Skip(skipCount);
            int tCount = 0;
            foreach (var table in tables)
            {
                //in each table, check the children
                var rowCells = table.SelectNodes("tr/td");

                if (rowCells == null || rowCells.Count == 0)
                    continue;

                //first row
                var appNameNode = rowCells[0].SelectSingleNode("span/strong/span");
                if (appNameNode == null || (appNameNode.ChildNodes.Count > 0 && String.IsNullOrEmpty(appNameNode.ChildNodes[0].GetDirectInnerText().Replace("\r\n", "").Trim())))
                {
                    //corner case <p><span><strong>app name</strong></span></p>
                    appNameNode = rowCells[0].SelectSingleNode("p/span/strong");
                }
                //another fallback case
                if (appNameNode == null || (appNameNode.ChildNodes.Count > 0 && String.IsNullOrEmpty(appNameNode.ChildNodes[0].GetDirectInnerText().Replace("\r\n", "").Trim())))
                {
                    appNameNode = rowCells[0].SelectSingleNode("span/strong");
                }
                //This table has tr/td that we won't parse yet.
                if (appNameNode == null || appNameNode.ChildNodes.Count < 2)
                    continue;

                var appNameVal = string.Empty;
                var appTypeVal = string.Empty;
                int appNameChildIndex = 0;
                int appTypeChildIndex = 1;
                if (String.IsNullOrEmpty(appNameNode.ChildNodes[0].GetDirectInnerText().Replace("\r\n", "").Trim()))
                {
                    if (appNameNode.ChildNodes.Count == 5)
                    {
                        //5 is for corner case of <span><strong>test<a></a>test1<em>test2</em>test3</strong></span>
                        appNameChildIndex = 2;
                        appTypeChildIndex = 3;
                    }
                    else
                    {
                        //corner case <span><strong><strong></strong></strong></span>
                        appNameNode = rowCells[0].SelectSingleNode("span/strong/strong");

                        if (appNameNode == null)
                        {
                            //<span><strong><span></span></strong></span>
                            appNameNode = rowCells[0].SelectSingleNode("span/strong/span");
                        }

                        if (rowCells[0].SelectSingleNode("span/span/strong") != null && rowCells[0].SelectSingleNode("span/strong/span/em") != null)
                        {
                            //<tr><td colspan="2"><span><span><strong>Name</strong></span><span></span><strong><span><em>Type</em></span><span></span><br></strong></span></td></tr>
                            appNameVal = string.Join(' ', rowCells[0].SelectSingleNode("span/span/strong").GetDirectInnerText().Split("\r\n").Select(_ => _.Trim())).Trim().Replace("&nbsp;", "");
                            appTypeVal = string.Join(' ', rowCells[0].SelectSingleNode("span/strong/span/em").GetDirectInnerText().Split("\r\n").Select(_ => _.Trim())).Trim().Replace("&nbsp;", "");
                        }
                    }
                }

                t = new JObject();
                t.Add("id", tCount++);

                //fix html formatting
                appNameVal = (String.IsNullOrEmpty(appNameVal) ? appNameNode.ChildNodes[appNameChildIndex].GetDirectInnerText() : appNameVal);
                t.Add("appName", new JValue(appNameVal.Replace(" ", "").Replace("\r\n", " ").Replace("&nbsp;", "").Trim()));

                appTypeVal = (String.IsNullOrEmpty(appTypeVal) ? appNameNode.ChildNodes[appTypeChildIndex].GetDirectInnerText() : appTypeVal);
                if (String.IsNullOrEmpty(appTypeVal) || String.IsNullOrEmpty(appTypeVal.Replace("\r\n", "").Trim()))
                {
                    appTypeVal = string.Empty;
                    foreach (var child in appNameNode.ChildNodes[appTypeChildIndex].ChildNodes)
                    {
                        appTypeVal += string.Join(' ', child.GetDirectInnerText().Split("\r\n").Select(_ => _.Trim())).Trim();
                    }
                }
                t.Add("appType", (String.IsNullOrEmpty(appTypeVal) ? new JValue(appTypeVal) : new JValue(string.Join(' ', appTypeVal.Split("\r\n").Select(_ => _.Trim())).Trim())));

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
                        if (childNode == null)
                        {
                            //corner case <span><span><strong></strong></span></span>
                            childNode = curNode.SelectSingleNode("span/span/strong");
                        }
                        //format key
                        key = string.Join(' ', childNode.InnerText.Split("\r\n").Select(_ => _.Trim())).Trim().TrimEnd(':').Replace(" ", String.Empty);
                    }
                    else
                    {
                        childNode = curNode.SelectSingleNode("span");
                        if (childNode == null)
                        {
                            //corner case <p><span>stuff</span></p>
                            childNode = curNode.SelectSingleNode("p");
                        }
                        //corner case missing a value for the key.
                        if (childNode == null && !string.IsNullOrEmpty(key))
                        {
                            t.Add(key, new JValue(value));
                            key = value = string.Empty;

                            parsingKey = true;

                            continue;
                        }

                        //format value
                        if (childNode.InnerHtml.Contains("\r\n"))
                        {
                            value = string.Join(' ', childNode.InnerHtml.Split("\r\n").Select(_ => _.Trim())).Trim();
                        }
                        else
                        {
                            value = childNode.InnerHtml;
                            //corner case: no value and we have a key
                            if (String.IsNullOrEmpty(value))
                            {
                                t.Add(key, new JValue(value));
                                key = value = string.Empty;

                                parsingKey = true;
                                continue;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(key))
                    {
                        t.Add(key, new JValue(value));
                        key = value = string.Empty;
                    }

                    parsingKey = !parsingKey;
                }

                sb.AppendLine(t.ToString());
            }

            ret = sb.ToString();

            return ret;
        }
    }
}
