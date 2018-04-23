using Enterprisebot.Services;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Advanced;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using System.Text;
using Microsoft.Bot.Builder.Dialogs;

namespace Enterprisebot.Dialogs
{
    [Serializable]
    public class CreateMeetingRequestDialog : IDialog<object>
    {
        private const string AccessTokenDataKey = "AccessToken";
        private const string MeetingDataKey = "MeetingData";
        private const string PossibleTimesKey = "PossibleTimes";
        private const string ChosenSlotKey = "ChosenSlot";

        public async Task StartAsync(IDialogContext context)
        {
            Trace.TraceInformation("CreateMeetingRequestDialog::StartAsync");

            await context.PostAsync("Processing Meeting Request ...");

            MeetingRequestInput meetingData = context.ConversationData.GetValue<MeetingRequestInput>(MeetingDataKey);

            context.Call<MeetingRequestInput>(new FormDialog<MeetingRequestInput>(
                meetingData,
                BuildMeetingRequestInputForm,
                FormOptions.PromptInStart),
                this.ResumeAfterMeetingRequestInputForm);
        }

        private async Task ResumeAfterMeetingRequestInputForm(IDialogContext context, IAwaitable<MeetingRequestInput> result)
        {
            // Let's figure out what's missing and ask the user.
            string accessToken = context.PrivateConversationData.GetValue<string>(AccessTokenDataKey);

            MeetingRequestInput meetingData = await result;
            ICalendarOperations ico = ServiceLocator.GetCalendarOperations();

            Dictionary<string, MeetingSlot> possibleTimes = await ico.FindMeetingTimes(meetingData, accessToken);

            context.ConversationData.SetValue(PossibleTimesKey, possibleTimes);
            context.ConversationData.SetValue(MeetingDataKey, meetingData);

            PromptDialog.Choice(context,
                this.OnPickMeetingTime,
                possibleTimes.Keys.ToArray(),
                "Please pick a meeting time:",
                "Invalid Choice", 2);
        }

        private async Task OnPickMeetingTime(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                string optionSelected = await result;
                Dictionary<string, MeetingSlot> possibleTimes =
                    context.ConversationData.GetValue<Dictionary<string, MeetingSlot>>(PossibleTimesKey);

                MeetingSlot slot = possibleTimes[optionSelected];
                context.ConversationData.SetValue(ChosenSlotKey, slot);

                MeetingRequestInput meetingData = context.ConversationData.GetValue<MeetingRequestInput>(MeetingDataKey);

                IMessageActivity msg = context.MakeMessage();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("*Please review your meeting request details:* \n\n");
                sb.AppendLine("\n\n **Why**: " + meetingData.MeetingSubject);
                sb.AppendLine(string.Format("\n\n **With whom**: {0} ({1})", meetingData.AttendeeName, meetingData.AttendeeEmail));
                sb.AppendLine("\n\n **When**: " + slot.Start.ToString());
                sb.AppendLine(string.Format("\n\n **Duration**: {0} minutes", (meetingData.MeetingDuration / 60).ToString()));
                msg.Text = sb.ToString();

                await context.PostAsync(msg);

                PromptDialog.Choice(context,
                    this.OnConfirmBooking,
                    new[] { "Yes", "No" },
                    "Would you like me to create the meeting request?",
                    "Invalid Choice", 2);
            }
            catch (TooManyAttemptsException ex)
            {
                string fullError = ex.ToString();
                Trace.TraceError(fullError);

                await context.PostAsync($"Sorry, I don't understand.");

                context.Done(true);
            }
        }

        private async Task OnConfirmBooking(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                string optionSelected = await result;
                if (optionSelected == "Yes")
                {
                    string accessToken = context.PrivateConversationData.GetValue<string>(AccessTokenDataKey);
                    MeetingRequestInput meetingData = context.ConversationData.GetValue<MeetingRequestInput>(MeetingDataKey);
                    MeetingSlot slot = context.ConversationData.GetValue<MeetingSlot>(ChosenSlotKey);

                    ICalendarOperations ico = ServiceLocator.GetCalendarOperations();
                    var results = await ico.MakeAppointment(meetingData, slot, accessToken);

                    await context.PostAsync(results);
                }
                else
                {
                    await context.PostAsync("OK, booking cancelled");
                }
                context.Done<object>(null);
            }
            catch (TooManyAttemptsException ex)
            {
                string fullError = ex.ToString();
                Trace.TraceError(fullError);

                await context.PostAsync($"Sorry, I don't understand.");

                context.Done(true);
            }
        }

        private static IForm<MeetingRequestInput> BuildMeetingRequestInputForm()
        {
            Field<MeetingRequestInput> durationField = new FieldReflector<MeetingRequestInput>(nameof(MeetingRequestInput.MeetingDuration));
            // TODO: Seems this is a good chance for an enum
            durationField.SetPrompt(new PromptAttribute("What is the meeting duration?"));
            // HACK: Meeting time is always an hour
            durationField.SetLimits(60, 3600);

            return new FormBuilder<MeetingRequestInput>()
                .Field(nameof(MeetingRequestInput.RequestedDateTime), "What day would you like to meet?")
                .Field(durationField)
                .Field(nameof(MeetingRequestInput.AttendeeEmail), "What is e-mail address of the person you'd like to meet?")
                .Field(nameof(MeetingRequestInput.AttendeeName), "What is the name of the person you're meeting?")
                .Field(nameof(MeetingRequestInput.MeetingSubject), "What is the meeting about?")
                .Build();
        }
    }
}