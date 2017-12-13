using CommerceBot.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CommerceBot.Dialogs
{
    [Serializable]
    public class CabanaReservationDialog : IDialog<object>
    {
        private const string UserSessionDataKey = "userdata";
        private string reservationChoice;
        public async Task StartAsync(IDialogContext context)
        {
            Trace.TraceInformation("CabanaReservationDialog::StartAsync");

            await context.PostAsync("Welcome to Hotel Services!");

            UserProfile userInfo = context.ConversationData.GetValue<UserProfile>(UserSessionDataKey);
            IHotelReservationOperations ihro = ServiceLocator.GetHotelReservationOperations();
            IList<string> menuOptions = await ihro.GetExistingReservations(userInfo.Id);

            PromptDialog.Choice(context, OnOptionSelected, menuOptions,
                    "Please choose your reservation:",
                    "Not a valid option", 2);
        }

        private async Task OnOptionSelected(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                Trace.TraceInformation("AppAuthDialog::OnOptionSelected");
                string optionSelected = await result;

                UserProfile userInfo = context.ConversationData.GetValue<UserProfile>(UserSessionDataKey);
                userInfo.ActiveReservation = optionSelected;
                reservationChoice = optionSelected;
                context.ConversationData.SetValue<UserProfile>(UserSessionDataKey, userInfo);

                var cabanaFormDialog = FormDialog.FromForm(this.BuildCabanaForm, FormOptions.PromptInStart);
                context.Call(cabanaFormDialog, this.ResumeAfterCabanaFormDialog);
            }
            catch (TooManyAttemptsException ex)
            {
                string fullError = ex.ToString();
                Trace.TraceError(fullError);

                await context.PostAsync($"Sorry, I don't understand.");

                context.Done(true);
            }
        }

        private IForm<CabanaQuery> BuildCabanaForm()
        {
            return new FormBuilder<CabanaQuery>()
                .Field(nameof(CabanaQuery.Start))
                .AddRemainingFields()
                .Build();
        }

        private async Task OnPickCabana(IDialogContext context, IAwaitable<string> result)
        {
            string optionSelected = await result;
            string cabanaIdText = optionSelected.Substring(optionSelected.LastIndexOf(' ') + 1);
            int cabanaId = int.Parse(cabanaIdText);

            await context.PostAsync($"Booking your cabana '{optionSelected}', please wait ...");

            CabanaQuery searchQuery = context.ConversationData.GetValue<CabanaQuery>("CabanaQuery");
            int reservationId = 1;

            DateTime startDate = searchQuery.Start;
            int days = searchQuery.Days;

            UserProfile userInfo = context.ConversationData.GetValue<UserProfile>(UserSessionDataKey);
            ICabanaReservationOperations icro = ServiceLocator.GetCabanaReservationOperations();
            CabanaReservation cres = await icro.ReserveCabana(reservationId, cabanaId, startDate, days);

            await context.PostAsync($"Cabana booked. Your Cabana Booking Id is {cres.CabanaBookingId}.");

            context.Done<object>(null);
        }

        private async Task ResumeAfterCabanaFormDialog(IDialogContext context, IAwaitable<CabanaQuery> result)
        {
            try
            {
                CabanaQuery searchQuery = await result;
                context.ConversationData.SetValue("CabanaQuery", searchQuery);

                await context.PostAsync($"Ok. Searching for Cabanas starting {searchQuery.Start.ToString("MM/dd")} " +
                    $"to {searchQuery.Start.AddDays(searchQuery.Days).ToString("MM/dd")}...");

                ICabanaReservationOperations icro = ServiceLocator.GetCabanaReservationOperations();
                IList<Cabana> cabanas = await icro.GetCabanaAvailability(reservationChoice, searchQuery);

                await context.PostAsync($"I found in total {cabanas.Count()} cabanas for your dates:");

                var resultMessage = context.MakeMessage();
                resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                resultMessage.Attachments = new List<Attachment>();

                List<string> cabanaChoices = new List<string>();
                foreach (var cabana in cabanas)
                {
                    cabanaChoices.Add(cabana.Name);
                    HeroCard heroCard = new HeroCard()
                    {
                        Title = cabana.Name,
                        Subtitle = $"{cabana.Rating} stars. {cabana.NumberOfReviews} reviews. From ${cabana.PriceStarting} per day.",
                        Images = new List<CardImage>()
                        {
                            new CardImage() { Url = cabana.Image }
                        },
                        Buttons = new List<CardAction>()
                        {
                            new CardAction()
                            {
                                Title = "More details",
                                Type = ActionTypes.OpenUrl,
                                Value = $"https://www.bing.com/search?q=hotels+in+" + HttpUtility.UrlEncode(cabana.Location)
                            }
                        }
                    };

                    resultMessage.Attachments.Add(heroCard.ToAttachment());
                }

                await context.PostAsync(resultMessage);
                cabanaChoices.Sort((h1, h2) => h1.CompareTo(h2));

                PromptDialog.Choice(context, this.OnPickCabana, cabanaChoices,
                   "Please pick your cabana:",
                   "Not a valid option", 3);
            }
            catch (FormCanceledException ex)
            {
                string reply;

                if (ex.InnerException == null)
                {
                    reply = "You have canceled the operation. Quitting from the CabanaDialog";
                }
                else
                {
                    reply = $"Oops! Something went wrong :( Technical Details: {ex.InnerException.Message}";
                }

                await context.PostAsync(reply);
            }
        }

    }
}