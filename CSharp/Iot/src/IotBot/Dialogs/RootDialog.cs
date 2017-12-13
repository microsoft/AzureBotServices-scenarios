using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace IotBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        // You'll need to create four Applets at IFTTT that can talk to your Hue
        private const string key = "Need Key!";
        private static string MakeHueUrl(string command) =>
            $"https://maker.ifttt.com/trigger/{command}/with/key/{key}";

        // Commands backed by Applets at IFTTT
        private const string cmdOn = "on";
        private const string cmdOff = "off";
        private const string cmdSetColor = "setcolor";
        private const string cmdChangeLevel = "changelevel";
        // Local Commands 
        private const string cmdHelp = "help";
        private const string cmdHello = "hello"; // <-- Add LUIS if you want richer options

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            string userText = activity.Text.ToLower();
            string userCmd = activity.Text.ToLower();
            string userSubCmd = string.Empty;

            string command = "LightsOff";
            bool sendCmd = true;

            if (userText.Contains(" "))
            {
                string[] userWords = userText.Split(' ');
                userCmd = userWords[0];
                userSubCmd = userWords[1];
            }

            switch (userCmd)
            {
                case cmdOn:
                    command = "LightsOn";
                    break;
                case cmdOff:
                    command = "LightsOff";
                    break;
                case cmdSetColor:
                    command = "SetColor";
                    break;
                case cmdChangeLevel:
                    command = "ChangeLevel";
                    break;
                default:
                    sendCmd = false;
                    break;
            }

            if (sendCmd)
            {
                // call Hue via IFTTT
                await CallHue(context, activity, command, userSubCmd);
            }
            else
            {
                // check for local commands
                switch (userCmd)
                {
                    case cmdHelp:
                        await ShowHelp(context, activity);
                        break;
                    case cmdHello:
                        await context.PostAsync("Hello, I hope you're well. Ask for 'help' or send me a Hue command.");
                        break;
                    default:
                        await context.PostAsync(activity.Text + " is not a command I know!");
                        break;
                }
            }

            context.Wait(MessageReceivedAsync);
        }

        private async Task CallHue(IDialogContext context, Activity activity, string command, string subcommand)
        {
            string cmdUrl = MakeHueUrl(command);

            await context.PostAsync("Talking to Hue via IFTTT ...");

            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, cmdUrl))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    if (!string.IsNullOrEmpty(subcommand))
                    {
                        JObject body = new JObject();
                        body.Add(new JProperty("value1", subcommand));

                        var httpContent = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
                        request.Content = httpContent;
                    }

                    using (var response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            Trace.TraceInformation(activity.Text + " was a success!");
                            string responseText = await response.Content.ReadAsStringAsync();
                            await context.PostAsync(responseText);
                        }
                    }
                }
            }
        }

        private async Task ShowHelp(IDialogContext context, Activity activity)
        {
            Activity reply = activity.CreateReply("I understand four commands:");
            reply.Type = ActivityTypes.Message;
            reply.TextFormat = TextFormatTypes.Plain;

            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                                {
                                    new CardAction(){ Title = "On", Type=ActionTypes.ImBack, Value="On" },
                                    new CardAction(){ Title = "Off", Type=ActionTypes.ImBack, Value="Off" },
                                    new CardAction(){ Title = "SetColor", Type=ActionTypes.ImBack, Value="SetColor Red" },
                                    new CardAction(){ Title = "ChangeLevel", Type=ActionTypes.ImBack, Value="ChangeLevel 75" }
                                }
            };
            await context.PostAsync(reply);
        }
    }
}