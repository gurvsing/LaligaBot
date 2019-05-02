using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BasicBot;
using Microsoft.Bot.Builder;

namespace Microsoft.BotBuilderSamples
{
    public class QnAServiceHelper
    {
        private QnAMakerEndpoint _endpoint;
        private QnAMakerOptions _options;
        private HttpClient _httpClient;

        public QnAServiceHelper()
        {
            this._httpClient = new HttpClient();
            this.InitQnAService();
        }

        public async Task<QnAResult[]> QueryQnAService(string query, QnABotState qnAcontext, ITurnContext turnContext)
        {
            var requestUrl = $"{this._endpoint.Host}/knowledgebases/{this._endpoint.KnowledgeBaseId}/generateanswer";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            var requestObject = new
            {
                question = query,
                top = this._options.Top,
                context = qnAcontext,
                strictFilters = this._options.StrictFilters,
                metadataBoost = this._options.MetadataBoost,
                scoreThreshold = this._options.ScoreThreshold,
            };
            var jsonRequest = JsonConvert.SerializeObject(requestObject, Formatting.None);

            request.Headers.Add("Authorization", $"EndpointKey {this._endpoint.EndpointKey}");
            request.Content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

            await LuisServiceV3.LogCustomTrace(requestObject, turnContext, "QnA Request", "QnA");

            var response = await this._httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var contentString = response.Content.ReadAsStringAsync().Result;

            var result = JsonConvert.DeserializeObject<QnAResultList>(contentString);
            await LuisServiceV3.LogCustomTrace(result, turnContext, "QnA Result", "QnA");

            return result.Answers;
        }

        private void InitQnAService()
        {
            this._options = new QnAMakerOptions
            {
                Top = 3,
            };

            this._endpoint = new QnAMakerEndpoint
            {
                KnowledgeBaseId = "c25afea7-0ca0-44e7-a630-cce73b121087",
                EndpointKey = "d3e7780b-fa03-4366-ab55-eaed70e97de4",
                Host = "https://rokulka-test.azurewebsites.net/qnamaker",
            };
        }
    }
}
