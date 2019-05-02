// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BasicBot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class BasicBot : IBot
    {
        // Supported LUIS Intents
        public const string FindMatchIntent = "FindMatch";
        public const string PurchaseTicket = "PurchaseTicket";
        public const string NoneIntent = "None";

        /// <summary>
        /// Key in the bot config (.bot file) for the LUIS instance.
        /// In the .bot file, multiple instances of LUIS can be configured.
        /// </summary>
        public static readonly string LuisConfiguration = "BasicBotLuisApplication";

        private readonly IStatePropertyAccessor<GreetingState> _greetingStateAccessor;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly IStatePropertyAccessor<QnABotState> _qnaStateAccessor;
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private readonly BotServices _services;
        private readonly LuisServiceV3 luisServiceV3;
        private readonly QnAServiceHelper qnAServiceHelper;
        private readonly LaLigaBL laLigaBL;

        private static bool InQnaMaker { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicBot"/> class.
        /// </summary>
        /// <param name="botServices">Bot services.</param>
        /// <param name="accessors">Bot State Accessors.</param>
        public BasicBot(BotServices services, UserState userState, ConversationState conversationState, ILoggerFactory loggerFactory)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            _qnaStateAccessor = _userState.CreateProperty<QnABotState>(nameof(QnABotState));
            _greetingStateAccessor = _userState.CreateProperty<GreetingState>(nameof(GreetingState));
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));

            // Verify LUIS configuration.
            if (!_services.LuisServices.ContainsKey(LuisConfiguration))
            {
                throw new InvalidOperationException($"The bot configuration does not contain a service type of `luis` with the id `{LuisConfiguration}`.");
            }

            laLigaBL = new LaLigaBL();
            luisServiceV3 = new LuisServiceV3();
            qnAServiceHelper = new QnAServiceHelper();

            Dialogs = new DialogSet(_dialogStateAccessor);
            Dialogs.Add(new GreetingDialog(_greetingStateAccessor, loggerFactory));
        }

        private DialogSet Dialogs { get; set; }

        /// <summary>
        /// Run every turn of the conversation. Handles orchestration of messages.
        /// </summary>
        /// <param name="turnContext">Bot Turn Context.</param>
        /// <param name="cancellationToken">Task CancellationToken.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity;

            // Create a dialog context
            var dc = await Dialogs.CreateContextAsync(turnContext);

            if (activity.Type == ActivityTypes.Message)
            {
                // Checks if status is currently inside QnA Maker multi turn situation
                if (InQnaMaker)
                {
                    var qnaResponse = await this.GetQnAResponse(activity.Text, turnContext);
                    await turnContext.SendActivityAsync(qnaResponse);
                    await _conversationState.SaveChangesAsync(turnContext);
                    await _userState.SaveChangesAsync(turnContext);
                    return;
                }

                // Perform a call to LUIS to retrieve results for the current activity message.
                // Until LUIS API v3 is integrated, we will call the v3 endpoint directly using a created service
                var luisResults = await luisServiceV3.PredictLUIS(turnContext.Activity.Text, dc.Context);

                // Continue the current dialog
                var dialogResult = await dc.ContinueDialogAsync();

                // if no one has responded,
                if (!dc.Context.Responded)
                {
                    // loop through all the LUIS predictions, these can be single or multiple if multi-intent is enabled
                    foreach (var response in luisResults)
                    {
                        var topIntent = response.Intent;

                        // examine results from active dialog
                        switch (dialogResult.Status)
                        {
                            case DialogTurnStatus.Empty:
                                switch (topIntent)
                                {
                                    case PurchaseTicket:
                                        var purchaseTicketResponse = laLigaBL.PurchaseTicket(response);
                                        await turnContext.SendActivityAsync(CardHelper.GetLUISHeroCard(purchaseTicketResponse, LaLigaBL.PictureType.Ticket));
                                        break;

                                    case FindMatchIntent:
                                        var findMatchResponse = laLigaBL.FindMatch(response);
                                        await turnContext.SendActivityAsync(CardHelper.GetLUISHeroCard(findMatchResponse.ResponseText, findMatchResponse.PictureType));
                                        break;

                                    // Redirect to QnA if None intent is detected
                                    case NoneIntent:
                                    default:
                                        var qnaResponse = await GetQnAResponse(activity.Text, turnContext);
                                        await turnContext.SendActivityAsync(qnaResponse);
                                        InQnaMaker = true;
                                        break;
                                }

                                break;

                            case DialogTurnStatus.Waiting:
                                // The active dialog is waiting for a response from the user, so do nothing.
                                break;

                            case DialogTurnStatus.Complete:
                                await dc.EndDialogAsync();
                                break;

                            default:
                                await dc.CancelAllDialogsAsync();
                                break;
                        }
                    }
                }
            }
            else if (activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (activity.MembersAdded != null)
                {
                    // Iterate over all new members added to the conversation.
                    foreach (var member in activity.MembersAdded)
                    {
                        // Greet anyone that was not the target (recipient) of this message.
                        // To learn more about Adaptive Cards, see https://aka.ms/msbot-adaptivecards for more details.
                        if (member.Id != activity.Recipient.Id)
                        {
                            var welcomeCard = CreateAdaptiveCardAttachment();
                            var response = CreateResponse(activity, welcomeCard);
                            await dc.Context.SendActivityAsync(response);
                        }
                    }
                }
                InQnaMaker = false; // Reset QnA Maker
            }

            await _conversationState.SaveChangesAsync(turnContext);
            await _userState.SaveChangesAsync(turnContext);
        }

        // Create an attachment message response.
        private Activity CreateResponse(Activity activity, Attachment attachment)
        {
            var response = activity.CreateReply();
            response.Attachments = new List<Attachment>() { attachment };
            return response;
        }

        // Load attachment from file.
        private Attachment CreateAdaptiveCardAttachment()
        {
            var adaptiveCard = File.ReadAllText(@".\Dialogs\Welcome\Resources\welcomeCard.json");
            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
            };
        }

        private async Task<Activity> GetQnAResponse(string query, ITurnContext turnContext)
        {
            Activity outputActivity = null;
            var newState = new QnABotState();

            var qnaState = await _qnaStateAccessor.GetAsync(turnContext, () => new QnABotState());
            var qnaResult = await this.qnAServiceHelper.QueryQnAService(query, qnaState, turnContext);
            var qnaAnswer = qnaResult[0].Answer;

            if (string.Equals(qnaAnswer, "No good match found in KB.", StringComparison.OrdinalIgnoreCase))
            {
                qnaAnswer = "I didn't understand what you just said to me.";
            }

            var prompts = qnaResult[0].Context?.Prompts;

            if (prompts == null || prompts.Length < 1)
            {
                outputActivity = MessageFactory.Text(qnaAnswer);
                InQnaMaker = false;
            }
            else
            {
                // Set bot state only if prompts are found in QnA result
                newState.PreviousQnaId = qnaResult[0].Id;
                newState.PreviousUserQuery = query;

                outputActivity = CardHelper.GetHeroCard(qnaAnswer, prompts);
            }

            await _qnaStateAccessor.SetAsync(turnContext, newState);

            return outputActivity;
        }

        /// <summary>
        /// Helper function to update greeting state with entities returned by LUIS.
        /// </summary>
        /// <param name="luisResult">LUIS recognizer <see cref="RecognizerResult"/>.</param>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        private async Task UpdateGreetingState(RecognizerResult luisResult, ITurnContext turnContext)
        {
            if (luisResult.Entities != null && luisResult.Entities.HasValues)
            {
                // Get latest GreetingState
                var greetingState = await _greetingStateAccessor.GetAsync(turnContext, () => new GreetingState());
                var entities = luisResult.Entities;

                // Supported LUIS Entities
                string[] userNameEntities = { "userName", "userName_patternAny" };
                string[] userLocationEntities = { "userLocation", "userLocation_patternAny" };

                // Update any entities
                // Note: Consider a confirm dialog, instead of just updating.
                foreach (var name in userNameEntities)
                {
                    // Check if we found valid slot values in entities returned from LUIS.
                    if (entities[name] != null)
                    {
                        // Capitalize and set new user name.
                        var newName = (string)entities[name][0];
                        greetingState.Name = char.ToUpper(newName[0]) + newName.Substring(1);
                        break;
                    }
                }

                foreach (var city in userLocationEntities)
                {
                    if (entities[city] != null)
                    {
                        // Capitalize and set new city.
                        var newCity = (string)entities[city][0];
                        greetingState.City = char.ToUpper(newCity[0]) + newCity.Substring(1);
                        break;
                    }
                }

                // Set the new values into state.
                await _greetingStateAccessor.SetAsync(turnContext, greetingState);
            }
        }
    }
}
