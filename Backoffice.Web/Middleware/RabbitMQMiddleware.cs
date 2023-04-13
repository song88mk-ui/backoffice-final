using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client.Events;
using System.Drawing;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Backoffice.Web.Middleware
{
    public class RabbitMQMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMessageBroker _messageBroker;
        private readonly IHubContext<BoHub> _boHub;
        private readonly string _region = "";

        public RabbitMQMiddleware(RequestDelegate next, IMessageBroker messageBroker, IHubContext<BoHub> boHub, IConfiguration config, string region = "")
        {
            _next = next;
            _messageBroker = messageBroker;
            _region = region.ToUpper();

            if (string.IsNullOrWhiteSpace(region))
            {
                _messageBroker.Connect(config?.GetSection($"MessageBroker:{_region}").Get<MessageBrokerCredentialModel>());
            }
            else
            {
                _messageBroker.Connect();
            }

            _messageBroker.Connect();
            _messageBroker.MessageReceived += MessageReceived;

            _boHub = boHub;

        }

        public async Task Invoke(HttpContext httpContext)
        {
            await _next(httpContext);
        }

        private async void MessageReceived(object sender, BasicDeliverEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.Body);
            var messageType = e.BasicProperties.Type;

            try
            {
                var hubClients = string.IsNullOrWhiteSpace(_region) ? _boHub.Clients.All : _boHub.Clients.Group(_region);

                if (e.RoutingKey.StartsWith("r.oe.bm.engine."))
                {
                    switch (messageType)
                    {
                        case "OnOddsChange":
                            var oddschange = JsonSerializer.Deserialize<OnExternalOddsChangeMessage>(payload);
                            await hubClients.SendAsync("onoddschange", oddschange);
                            break;
                        case "OnBetStopFixture":
                            var betStopFixture = JsonSerializer.Deserialize<OnExternalBetStopFixtureMessage>(payload);
                            await hubClients.SendAsync("onbetstopfixture", betStopFixture);
                            break;
                        case "OnBetStopGlobal":
                            var betStopGlobal = JsonSerializer.Deserialize<OnExternalBetStopGlobalMessage>(payload);
                            await hubClients.SendAsync("onbetstopglobal", betStopGlobal);
                            break;
                        case "OnSignIn":
                            var signin = JsonSerializer.Deserialize<OnExternalOddsChangeMessage>(payload);
                            await hubClients.SendAsync("onsignin", signin);
                            break;
                        case "OnNewNotification":
                            var notificationsModel = JsonSerializer.Deserialize<NotificationsModel>(payload);
                            await hubClients.SendAsync("onnewnotification", notificationsModel);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
