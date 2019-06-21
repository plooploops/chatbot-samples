
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Text;
using System;
using System.Collections.Generic;

namespace json2csv
{
    public static class json2csvconvert
    {
        [FunctionName("Function1")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            StringBuilder fileContent = new StringBuilder();

            log.Info("C# HTTP trigger function processed a request.");

            var jsonMessage = Convert.FromBase64String(new StreamReader(req.Body).ReadToEnd());
            var fileOutput = JsonConvert.DeserializeObject<Dictionary<string, string>>(Encoding.UTF8.GetString(jsonMessage));

            foreach (var item in fileOutput)
            {
                fileContent.AppendLine(item.Key + "," + item.Value + ",");
            }

            fileContent.Remove(fileContent.Length - 3, 1);
            log.Info(">> fileContent is: {fileContent}");

            return (ActionResult)new OkObjectResult(fileContent.ToString());
        }
    }
}
