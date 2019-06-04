using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EchoBot1.Services
{
    public class QnAService
    {
        // NOTE: Replace this with a valid host name.
        static string host = "";

        // NOTE: Replace this with a valid endpoint key.
        // This is not your subscription key.
        // To get your endpoint keys, call the GET /endpointkeys method.
        static string endpoint_key = "";

        // NOTE: Replace this with a valid knowledge base ID.
        // Make sure you have published the knowledge base with the
        // POST /knowledgebases/{knowledge base ID} method.
        static string kb = "";

        static string service = "/qnamaker";
        static string method = "/knowledgebases/" + kb + "/generateAnswer/";

        public async static Task<string> PostQuestion(string question)
        {
            string body = $"{{\"question\" : \"{question}\"}}";
            string uri = host + service + method;
            return await Post(uri, body);
        }

        async static Task<string> Post(string uri, string body)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                request.Headers.Add("Authorization", "EndpointKey " + endpoint_key);

                var response = await client.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
