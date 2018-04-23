using Enterprisebot.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Enterprisebot.Dialogs
{
    [Serializable]
    [LuisModel("", "")]
    public class AppRootDialog : LuisDialog<object>
    {
        // Names of Entities from LUIS Model
        private const string EntityWhen = "builtin.datetimeV2.date";
        private const string EntityHowLong = "builtin.datetimeV2.duration";
        private const string EntityEmail = "builtin.email";
        private const string EntityTitle = "Note.Title";
        private const string MeetingDataKey = "MeetingData";

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as IMessageActivity;

            context.Wait(MessageReceivedAsync);
        }

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string message = $"Sorry, I did not understand '{result.Query}'. Type 'help' if you need assistance.";

            await context.PostAsync(message);

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("Hello")]
        public async Task Greetings(IDialogContext context, LuisResult result)
        {
            string message = $"Hi! I hope you're well. What can I do for you? Type 'help' for ideas.";

            await context.PostAsync(message);

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("Help")]
        public async Task Help(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Hi! Try asking to 'Make an appointment'.");

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("Calendar.CheckAvailability")]
        public async Task CalendarLookup(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            Trace.TraceInformation("AppRootDialog::CalendarLookup");

            await context.PostAsync("Reviewing your request ...");

            MeetingRequestInput meetingData = new MeetingRequestInput();
            if (result.TryFindEntity(EntityHowLong, out EntityRecommendation recHowLong))
            {
                recHowLong.Type = "HowLong";
                meetingData.MeetingDuration = GetRecommendationValue<int>(recHowLong, "value");
            }
            if (result.TryFindEntity(EntityWhen, out EntityRecommendation recWhen))
            {
                recWhen.Type = "When";
                meetingData.RequestedDateTime = GetRecommendationValues<DateTime>(recWhen, "value").First(v => v > DateTime.UtcNow);
            }

            if (result.TryFindEntity(EntityEmail, out EntityRecommendation recEmail))
            {
                recEmail.Type = "Who";
                meetingData.AttendeeEmail = recEmail.Entity;
            }

            if (result.TryFindEntity(EntityTitle, out EntityRecommendation recTitle))
            {
                recTitle.Type = "What";
                meetingData.MeetingSubject = recTitle.Entity;
            }

            context.ConversationData.SetValue(MeetingDataKey, meetingData);
            var message = await activity;

            await context.Forward(new AppAuthDialog(),
                this.ResumeAfterCalendarCheckDialog, message, CancellationToken.None);
        }

        private static T GetRecommendationValue<T>(EntityRecommendation recommendation, string key)
        {
            return GetRecommendationValues<T>(recommendation, key).Single();
        }

        private static IEnumerable<T> GetRecommendationValues<T>(EntityRecommendation recommendation, string key)
        {
            List<object> resolutionList = (List<object>)recommendation.Resolution["values"];
            IEnumerable<Dictionary<string, object>> resolutionDictionaries = resolutionList.Cast<Dictionary<string, object>>();
            return resolutionDictionaries.Select(resolutionDictionary => (T)Convert.ChangeType(resolutionDictionary[key], typeof(T)));
        }

        private async Task ResumeAfterCalendarCheckDialog(IDialogContext context, IAwaitable<object> result)
        {
            await context.PostAsync("Thank you. We're all done. Is there anything else I can do for you?");

            context.Done<object>(null);
        }
    }
}