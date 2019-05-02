using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using BasicBot;
using Microsoft.Bot.Builder.LanguageGeneration;

namespace Microsoft.BotBuilderSamples
{
    public class CardHelper
    {
        /// <summary>
        /// Get Hero card
        /// </summary>
        /// <param name="cardTitle">Title of the card</param>
        /// <param name="prompts">List of suggested prompts</param>
        /// <returns>Message activity</returns>
        public static Activity GetHeroCard(string cardTitle, QnAPrompts[] prompts)
        {
            var chatActivity = Activity.CreateMessageActivity();
            var buttons = new List<CardAction>();

            var sortedPrompts = prompts.OrderBy(r => r.DisplayOrder);
            foreach (var prompt in sortedPrompts)
            {
                buttons.Add(
                    new CardAction()
                    {
                        Value = prompt.DisplayText,
                        Type = ActionTypes.ImBack,
                        Title = prompt.DisplayText,
                    });
            }

            var plCard = new HeroCard()
            {
                //Title = cardTitle,
                Subtitle = string.Empty,
                Buttons = buttons,
                Text = cardTitle,
            };

            var attachment = plCard.ToAttachment();
            
            chatActivity.Attachments.Add(attachment);
            
            return (Activity)chatActivity;
        }

        public static Activity GetLUISHeroCard(string text, LaLigaBL.PictureType picture)
        {
            var chatActivity = Activity.CreateMessageActivity();
            var lgEngine = TemplateEngine.FromFiles(@".\Resources\LaLigaTemplates.lg");

            string imageURL;
            switch (picture)
            {
                case LaLigaBL.PictureType.ChampionsLeague:
                    imageURL = lgEngine.EvaluateTemplate("ChampionsLeagueImage", new { });
                    break;
                case LaLigaBL.PictureType.LaLiga:
                    imageURL = lgEngine.EvaluateTemplate("CardImage", new { });
                    break;
                case LaLigaBL.PictureType.Ticket:
                    imageURL = lgEngine.EvaluateTemplate("TicketsImage", new { });
                    break;
                default:
                    imageURL = "";
                    break;
            }

            CardImage cardImage = new CardImage(imageURL);
            var plCard = new HeroCard()
            {
                //Title = cardTitle,
                Text = text,
                Images = new List<CardImage>() { cardImage }
            };
            var attachment = plCard.ToAttachment();

            chatActivity.Attachments.Add(attachment);

            return (Activity)chatActivity;
        }
    }
}
