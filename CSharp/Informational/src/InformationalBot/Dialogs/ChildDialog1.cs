using System;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Linq;

namespace InformationalBot.Dialogs
{
    [Serializable]
    [QnAMaker("subscriptionKey", "knowledgebaseId", "I don't understand this right now! Try another query!", 0.01, 1)]
    public class ChildDialog1 : QnAMakerDialog
    {
        protected override async Task RespondFromQnAMakerResultAsync(IDialogContext context, IMessageActivity message, QnAMakerResults results)
        {
            if (results.Answers.Count > 0)
            {
                var response = "Here is a match from our FAQ:  \r\n  Q: " +
                    results.Answers.First().Answer;
                await context.PostAsync(response);
            }
        }

        protected override async Task DefaultWaitNextMessageAsync(IDialogContext context, IMessageActivity message, QnAMakerResults results)
        {
            if (results.Answers.Count == 0)
            {
                Console.WriteLine("Sorry, I'm not able to find an answer for you.");
            }
            await base.DefaultWaitNextMessageAsync(context, message, results);
        }
    }
}