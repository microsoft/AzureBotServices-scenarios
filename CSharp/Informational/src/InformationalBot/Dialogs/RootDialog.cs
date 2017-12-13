using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;
using System.Linq;
using System.Threading;

namespace InformationalBot.Dialogs
{
    [Serializable]
    [QnAMaker("subscriptionKey", "knowledgebaseId", "Still looking ...", 0.01, 2)]
    public class RootDialog : QnAMakerDialog
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
                var childFaq = new ChildDialog1();
                await context.Forward(childFaq, AfterFAQDialog, message, CancellationToken.None);
            }
            else
            {
                await base.DefaultWaitNextMessageAsync(context, message, results);
            }
        }

        private Task AfterFAQDialog(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            context.Done<object>(null);
            return Task.CompletedTask;
        }
    }
}