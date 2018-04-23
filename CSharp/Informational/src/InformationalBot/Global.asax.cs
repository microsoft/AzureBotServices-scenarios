using Autofac;
using Autofac.Integration.WebApi;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using System.Reflection;
using System.Web.Http;

namespace InformationalBot
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private const string BotStateTableName = "InformationalBot";

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"];

            var config = GlobalConfiguration.Configuration;
            Conversation.UpdateContainer(
                builder =>
                {
                    builder.RegisterModule(new AzureModule(Assembly.GetExecutingAssembly()));

                    var store = new TableBotDataStore(connectionString, BotStateTableName);
                    builder.Register(c => store)
                        .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                        .AsSelf()
                        .SingleInstance();

                    // Register your Web API controllers.
                    builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
                    builder.RegisterWebApiFilterProvider(config);
                });

            config.DependencyResolver = new AutofacWebApiDependencyResolver(Conversation.Container);
        }
    }
}
