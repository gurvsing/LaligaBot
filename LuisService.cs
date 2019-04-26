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

        public LuisResponse(string intent, LuisServiceV3.V3App._Entities entities, string response) : this()
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
            Response = response;
        }
    }
    public class LuisServiceV3
    {
        public const string LuisTraceType = "https://www.luis.ai/schemas/trace";

        public const string LuisTraceLabel = "Luis Trace";

        public static string CoreferenceHome { get; set; } // = "Barcelona";

        public static HashSet<string> Coreferences = new HashSet<string>() { "they", "we", "them", "their" };

        public static string url = "https://dialogice4endpoint.cloudapp.net:8081/api/v3.0-preview/apps/9552ae0e-7d53-4cdc-a18e-eadd87320f77/slots/PRODUCTION/predict?log=true&subscription-key=123";

        public string subscriptionKey { get; set; }

        public string appId { get; set; }

        public LuisServiceV3()
        {
            subscriptionKey = "014fee3605e84fdc9d772b8092a7e7f4";
            appId = "3da9e60-fe37-4bfc-bb05-fef9eedd676f";
        }

        public async Task<LuisResponse> PredictLUIS(string query, ITurnContext turnContext)
        {
            using (var httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                using (var client = new HttpClient(httpClientHandler))
                {
                    
                    var postObject = JsonConvert.SerializeObject(CreateV3Request(query));
                    var requestContent = new StringContent(postObject, Encoding.UTF8, "application/json");

                    await this.LogLUISTrace(JObject.Parse(postObject), turnContext);

                    var response = await client.PostAsync(url, requestContent);
                    var responseStr = await response.Content.ReadAsStringAsync();
                    dynamic content = JObject.Parse(responseStr);

                    var v3AppConverter = new V3App();
                    V3App responseNew = v3AppConverter.Convert(content);
                    var beautified = content.ToString(Formatting.Indented);
                    await this.LogLUISTrace(content, turnContext);
                    var intent = responseNew.Prediction.TopIntent;
                    var entities = responseNew.Prediction.Entities;
                    return new LuisResponse(intent, entities, beautified);
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

                    {"overridePredictions", true }

                }
            };
            var tokens = Query.Split(" ");
            var tokenSet = new HashSet<string>(tokens);
            foreach (var reference in Coreferences)
            {
                if(tokenSet.Contains(reference))
                {
                    v3Request.externalEntities = new List<V3Request._ExternalEntities>()
                    {
                        new V3Request._ExternalEntities()
                        {
                            entityName = "Team",
                            startIndex = Query.IndexOf(reference),
                            entityLength = reference.Length,
                            resolution = new string[] { CoreferenceHome },
                        },
                    };
                    break;
                }
            }

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
                                    synonyms = new string[]{"ajax", "ajax fc" }
                                },
                                new V3Request._DynamicLists._RequestList()
                                {
                                    name = "Champions League Team",
                                    canonicalForm = "Tottenham Hotspurs",
                                    synonyms = new string[] {"tottenham", "spurs" }
                                },
                        },
                    }
                };

                LaLigaBL.IsChampionsLeague = true;
            }

            return v3Request;
        }

        public class V3Request
        {
            public string query { get; set; }

            public Dictionary<string, dynamic> options;

            public List<_ExternalEntities> externalEntities {get; set;}

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

        public async Task<Microsoft.Bot.Schema.ResourceResponse> LogLUISTrace(dynamic luisResult, ITurnContext turnContext)
        {
            var traceInfo = JObject.FromObject(
                new
                {
                    luisResult,
                });

            return await turnContext.TraceActivityAsync("LuisRecognizer", traceInfo, "Hello", "Luis Result").ConfigureAwait(false);
        }

        public class V3App
        {
            public string Text;

            public string AlteredText;

            public string Query;

            public _Prediction Prediction;

            public class _Prediction
            {
                public string NormalizedQuery;
                public string TopIntent;
                public Dictionary<Intent, IntentScore> Intents;
                public _Entities Entities;

            }
            public enum Intent
            {
                FindMatch,
                GetStatistics,
                None,
                PurchaseTicket
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
            public _Entities Entities;

            [JsonExtensionData(ReadData = true, WriteData = true)]
            public IDictionary<string, object> Properties { get; set; }

            public V3App Convert(dynamic result)
            {
                V3App app = JsonConvert.DeserializeObject<V3App>(JsonConvert.SerializeObject(result));
                Text = app.Text;
                AlteredText = app.AlteredText;
                Entities = app.Entities;
                Properties = app.Properties;
                return app;
            }



        }
    }
}
