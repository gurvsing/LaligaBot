using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.TraceExtensions;
using System.Text;

namespace BasicBot
{

    public class LuisServiceV3
    {
        public string url { get; set; }

        public string subscriptionKey { get; set; }

        public string appId { get; set; }

        public string slot { get; set; }
        public LuisServiceV3()
        {
            subscriptionKey = "014fee3605e84fdc9d772b8092a7e7f4";
            appId = "e011cee6-32a2-43df-bfcc-1979d87fd506";
            slot = "PRODUCTION";
            url = $"https://westus.api.cognitive.microsoft.com/luis/v3.0-preview/apps/{appId}/slots/{slot}/predict?subscription-key={subscriptionKey}&multiple-intents=true&log=true";
        }

        public static HashSet<string> Coreferences = new HashSet<string>() { "they", "we", "them", "their" };

        // Call endpoint to get prediction
        public async Task<List<LuisResponse>> PredictLUIS(string query, ITurnContext turnContext)
        {
            using (var httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                using (var client = new HttpClient(httpClientHandler))
                {
                    var postData = CreateV3Request(query); // Create request object
                    var postObject = JsonConvert.SerializeObject(postData); // Serialize data for request
                    var requestContent = new StringContent(postObject, Encoding.UTF8, "application/json"); // Setup Post Data

                    await LogCustomTrace(postData, turnContext, "LUIS Request", "LUIS"); // Log trace of request

                    var response = await client.PostAsync(url, requestContent); // Request prediction from LUIS endpoint
                    var responseStr = await response.Content.ReadAsStringAsync(); // Read response

                    //dynamic content = JObject.Parse(responseStr);
                    //var beautified = content.ToString(Formatting.Indented);

                    var v3AppConverter = new V3Response();
                    V3Response v3Response = JsonConvert.DeserializeObject<V3Response>(responseStr); // Deserialize response to class v3 response

                    //V3Response responseNew = v3AppConverter.Convert(content);

                    await LogCustomTrace(v3Response, turnContext, "LUIS Response", "LUIS"); // Log trace of response

                    var predictions = new List<LuisResponse>(); // Add LUIS response as intent and entities

                    if (v3Response.Prediction.Intents.ContainsKey(V3Response.Intent.MultipleIntents))
                    {
                        // If multiple intents exist, add their predictions
                        foreach (var prediction in v3Response.Prediction.Intents[V3Response.Intent.MultipleIntents].predictions)
                        {
                            var intent = prediction.TopIntent;
                            var entities = prediction.Entities;
                            predictions.Add(new LuisResponse(prediction.TopIntent, prediction.Entities));
                        }
                    }
                    else
                        predictions.Add(new LuisResponse(v3Response.Prediction.TopIntent, v3Response.Prediction.Entities));

                    return predictions;
                }
            }
        }

        public V3Request CreateV3Request(string Query)
        {
            var v3Request = new V3Request()
            {
                query = Query,

                options = new Dictionary<string, dynamic>()
                {

                },
                externalEntities = new List<V3Request._ExternalEntities>(),
                dynamicLists = new List<V3Request._DynamicLists>()
            };


            // Checks if query includes tokens that include coreferences and saves the coreference as an external entity of the last team saved in context in the La Liga Service
            var tokens = Query.Split(" ");
            var tokenSet = new HashSet<string>(tokens);
            foreach (var reference in Coreferences)
            {
                if (tokenSet.Contains(reference))
                {
                    v3Request.externalEntities = new List<V3Request._ExternalEntities>()
                    {
                        new V3Request._ExternalEntities()
                        {
                            entityName = "Team",
                            startIndex = Query.IndexOf(reference),
                            entityLength = reference.Length,
                            resolution = new string[] { LaLigaBL.CoreferenceTeam },
                        },
                    };
                    break;
                }
            }

            // Checks if Champions League is in utterance to trigger dynamic lists of champions league teams
            if (Query.ToLower().Contains("champions league"))
            {
                v3Request.dynamicLists = new List<V3Request._DynamicLists>()
                {
                    new V3Request._DynamicLists()
                    {
                        listEntityName = "Team",
                        requestLists = new List<V3Request._DynamicLists._RequestList>()
                        {
                                new V3Request._DynamicLists._RequestList()
                                {
                                    name = "Champions League Team",
                                    canonicalForm = "Liverpool",
                                    synonyms = new string[] { "liverpool", "livpool", "liver", "lfc" }
                                },
                                new V3Request._DynamicLists._RequestList()
                                {
                                    name = "Champions League Team",
                                    canonicalForm = "Ajax",
                                    synonyms = new string[]{ "ajax", "ajax fc" }
                                },
                                new V3Request._DynamicLists._RequestList()
                                {
                                    name = "Champions League Team",
                                    canonicalForm = "Tottenham Hotspurs",
                                    synonyms = new string[] { "tottenham", "spurs" }
                                },
                        },
                    }
                };

                LaLigaBL.IsChampionsLeague = true; // Sets CL flag for Find Match in LaLigaBL
            }

            return v3Request;
        }

        public class V3Request
        {
            [JsonProperty(Order = 1)]
            public string query { get; set; }

            [JsonProperty(Order = 2)]
            public Dictionary<string, dynamic> options;

            [JsonProperty(Order = 3)]
            public List<_ExternalEntities> externalEntities { get; set; }

            [JsonProperty(Order = 4)]
            public List<_DynamicLists> dynamicLists { get; set; }

            public class _ExternalEntities
            {
                public string entityName { get; set; }

                public int startIndex { get; set; }

                public int entityLength { get; set; }

                public dynamic resolution { get; set; }
            }

            public class _DynamicLists
            {
                public string listEntityName { get; set; }

                public List<_RequestList> requestLists { get; set; }

                public class _RequestList
                {
                    public string name { get; set; }

                    public string canonicalForm { get; set; }

                    public string[] synonyms { get; set; }
                }
            }
        }

        public class V3Response
        {
            [JsonProperty(Order = 1)]
            public string Query;

            [JsonProperty(Order = 2)]
            public _Prediction Prediction;

            public class _Prediction
            {
                [JsonProperty(Order = 1)]
                public string NormalizedQuery;
                [JsonProperty(Order = 2)]
                public string TopIntent;
                [JsonProperty(Order = 3)]
                public Dictionary<Intent, IntentsObject> Intents;
                [JsonProperty(Order = 4)]
                public _Entities Entities;

            }
            public enum Intent
            {
                FindMatch,
                None,
                PurchaseTicket,
                MultipleIntents
            };

            public class _Entities
            {
                // Simple entities
                public string[] MyTeam;

                // Built-in entities
                public double[] number;

                // Lists
                public string[][] AgeTier;
                public string[][] Team;
                public string[][] Home;
                public string[][] Away;
                public string[][] Relative;

                // Instance
                public class _Instance
                {
                    public InstanceData[] MyTeam;
                    public InstanceData[] number;
                    public InstanceData[] AgeTier;
                    public InstanceData[] Team;
                    public InstanceData[] Home;
                    public InstanceData[] Away;
                }
                [JsonProperty("$instance")]
                public _Instance _instance;
            }

            public class IntentsObject
            {
                [JsonProperty("score")]
                public double? Score { get; set; }

                [JsonProperty("predictions")]
                public List<_Prediction> predictions = new List<_Prediction>();
            }

            [JsonExtensionData(ReadData = true, WriteData = true)]
            public IDictionary<string, object> Properties { get; set; }

        }

        // Log Custom Trace
        public static async Task<Microsoft.Bot.Schema.ResourceResponse> LogCustomTrace(object trace, ITurnContext turnContext, string type, string service)
        {
            return await turnContext.TraceActivityAsync(service, trace, type, type).ConfigureAwait(false);
        }
    }

    public class LuisResponse
    {
        public string Intent { get; set; }

        public Dictionary<string, string> Entities { get; set; }

        public LuisResponse(string intent, LuisServiceV3.V3Response._Entities entities)
        {
            Intent = intent;
            Entities = new Dictionary<string, string>()
            {
                { "Home", entities.Home?[0]?[0] ?? null },
                { "Away", entities.Away?[0]?[0] ?? null },
                { "Team", entities.Team?[0]?[0] ?? null },
                { "number", entities.number?[0].ToString() },
                { "Relative", entities.Relative?[0]?[0] ?? null },

            };
        }
    }

}
