using BotAuth;
using BotAuth.AADv2;
using BotAuth.Models;
using Enterprisebot.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SD = System.Diagnostics;

namespace Enterprisebot.Dialogs
{
    [Serializable]
    public class AppAuthDialog : IDialog<object>
    {
        private static bool displayAuthMessages = false;
        private const string GraphUrl = "https://graph.microsoft.com/v1.0/me";
        private const string AccessTokenDataKey = "AccessToken";
        private const string MeetingDataKey = "MeetingData";

        public Task StartAsync(IDialogContext context)
        {
            SD.Trace.TraceInformation("AppAuthDialog::StartAsync");

            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(
            IDialogContext context,
            IAwaitable<object> awaitableMessage)
        {
            SD.Trace.TraceInformation("AppAuthDialog::MessageReceivedAsync");

            var activity = await awaitableMessage as Activity;
            await context.PostAsync("Let's make sure you're logged in ...");

            // Initialize AuthenticationOptions and forward to AuthDialog for token
            AuthenticationOptions options = new AuthenticationOptions()
            {
                ClientId = ConfigurationManager.AppSettings["aad:ClientId"],
                ClientSecret = ConfigurationManager.AppSettings["aad:ClientSecret"],
                Scopes = new string[] { "User.Read" },
                RedirectUrl = ConfigurationManager.AppSettings["aad:Callback"]
            };

            context.ConversationData.SetValue("Activity", activity);
            await context.Forward(
                new BotAuth.Dialogs.AuthDialog(new MSALAuthProvider(), options),
                this.AfterInitialAuthDialog,
                activity,
                CancellationToken.None);
        }

        private async Task AfterInitialAuthDialog(
            IDialogContext authContext,
            IAwaitable<AuthResult> awaitableAuthResult)
        {
            try
            {
                SD.Trace.TraceInformation("AppAuthDialog::AfterInitialAuthDialog");

                AuthResult authResult = await awaitableAuthResult;
                Activity activity = authContext.ConversationData.GetValue<Activity>("Activity");

                // return our reply to the user for debugging purposes
                if (displayAuthMessages)
                {
                    int length = (activity.Text ?? string.Empty).Length;
                    await authContext.PostAsync($"We see you sent {activity.Text} which was {length} characters");
                }

                if (authResult == null)
                {
                    await authContext.PostAsync("You didn't log in.");
                    authContext.Done(true);
                    return;
                }
                else
                {
                    if (displayAuthMessages)
                    {
                        await authContext.PostAsync($"Token: {authResult.AccessToken}");
                    }
                }

                // Use token to call into service
                JObject json = await new HttpClient().GetWithAuthAsync(
                    authResult.AccessToken, GraphUrl);

                // Two items to test
                // A -- Access Token Expires, do JUST [Part 1] Below
                // B -- Access Token Expires AND refresh fails, do [Part 1], [Part 2], and [Part 3].
                //
                // To test auth expiration null out json variable (uncomment next line) [Part 1]
                // json = null;
                if (json == null)
                {
                    var authProvider = new MSALAuthProvider();
                    AuthenticationOptions options =
                        authContext.UserData.GetValue<AuthenticationOptions>(
                            $"{authProvider.Name}{ContextConstants.AuthOptions}");

                    SD.Trace.TraceInformation("Attempting to refresh with token.");
                    if (displayAuthMessages)
                    {
                        await authContext.PostAsync($"Attempting to refresh with token: {authResult.RefreshToken}");
                    }

                    // To test auth expiration comment out next line [Part 2]
                    authResult = await authProvider.GetAccessToken(options, authContext);

                    // To test auth expiration uncomment out next two lines [Part 3]
                    // authResult = null;
                    // await authProvider.Logout(options, authContext);
                    if (authResult != null)
                    {
                        SD.Trace.TraceInformation("Token Refresh Succeeded.");
                        if (displayAuthMessages)
                        {
                            await authContext.PostAsync($"Token Refresh Succeeded. New Token: {authResult.AccessToken}");
                        }
                        json = await new HttpClient().GetWithAuthAsync(
                            authResult.AccessToken, "https://graph.microsoft.com/v1.0/me");
                    }
                    else
                    {
                        SD.Trace.TraceInformation("Token Refresh Failed. Trying full login.");

                        if (displayAuthMessages)
                        {
                            await authContext.PostAsync("Token Refresh Failed. Trying full login.");
                        }
                        await authContext.Forward(
                                        new BotAuth.Dialogs.AuthDialog(new MSALAuthProvider(), options),
                                        this.AfterInitialAuthDialog,
                                        activity,
                                        CancellationToken.None);
                        return;
                    }
                }

                SD.Trace.TraceInformation("Getting user data post auth.");
                string userName = json.Value<string>("displayName");
                string userEmail = json.Value<string>("userPrincipalName");

                if (displayAuthMessages)
                {
                    await authContext.PostAsync($"I now know your name is {userName} " +
                        $"and your UPN is {userEmail}");
                }

                MeetingRequestInput meetingData = authContext.ConversationData.GetValue<MeetingRequestInput>(MeetingDataKey);
                meetingData.OrganizerName = userName;
                meetingData.OrganizerEmail = userEmail;

                authContext.ConversationData.SetValue(MeetingDataKey, meetingData);
                authContext.PrivateConversationData.SetValue(AccessTokenDataKey, authResult.AccessToken);

                SD.Trace.TraceInformation("Post Auth Hand Off to CreateMeetingRequestDialog.");
                authContext.Call(new CreateMeetingRequestDialog(), this.ResumeAfterMeetingDialog);
            }
            catch (Exception ex)
            {
                string fullError = ex.ToString();
                SD.Trace.TraceError(fullError);
                await authContext.PostAsync(fullError);
            }
        }

        private async Task ResumeAfterMeetingDialog(IDialogContext context, IAwaitable<object> result)
        {
            try
            {
                SD.Trace.TraceInformation("AppAuthDialog::ResumeAfterMeetingDialog");

                var message = await result;
            }
            catch (Exception ex)
            {
                await context.PostAsync($"Failed with message: {ex.Message}");
            }
            finally
            {
                context.Done<object>(null);
            }
        }
    }
}