using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace BasicBot
{
    public struct LuisResponse
    {
        public string Intent { get; set; }

        public Dictionary<string, string> Entities { get; set; }

        public string Response { get; set; }

        public LuisResponse(string intent, Dictionary<string, string> entities, string response)
        {
            Entities = entities;
            Intent = intent;
            Response = response;
        }
    }
    public class LuisServiceV3
    {
        public static string url = "https://dialogice4endpoint.cloudapp.net:8081/api/v3.0-preview/apps/15fcc294-81e5-4c4b-8da8-41514b006c07/slots/PRODUCTION/predict?log=true&subscription-key=123&query=";

        public string subscriptionKey { get; set; }

        public string appId { get; set; }

        public LuisServiceV3()
        {
            subscriptionKey = "014fee3605e84fdc9d772b8092a7e7f4";
            appId = "3da9e60-fe37-4bfc-bb05-fef9eedd676f";
        }

        public async Task<LuisResponse> PredictLUIS(string query)
        {
            using (var httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                using (var client = new HttpClient(httpClientHandler))
                {
                    var uri = new Uri(url + query);
                    var response = await client.GetAsync(uri);
                    var responseStr = await response.Content.ReadAsStringAsync();
                    dynamic content = JObject.Parse(responseStr);
                    var beautified = content.ToString(Formatting.Indented);
                    var intent = content.prediction.topIntent.Value;
                    var entities = GetEntities(content.prediction.entities);
                    return new LuisResponse(intent, entities, beautified);
                }
            }
        }

        public Dictionary<string, string> GetEntities(dynamic entitiesObject)
        {
            var entities = new Dictionary<string, string>();
            foreach (var item in entitiesObject.Children())
            {
                if (item.Value?.First?.HasValues)
                {
                    entities.Add(item.Name, item.Value.First.First.Value);
                }
            }

            return entities;
        }
    }
}
