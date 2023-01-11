using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Backoffice.Web.Middleware
{
    public class RabbitMQMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMessageBroker _messageBroker;
        private readonly IHubContext<BoHub> _boHub;

        public RabbitMQMiddleware(RequestDelegate next, IMessageBroker messageBroker, IHubContext<BoHub> boHub)
        {
            _next = next;
            _messageBroker = messageBroker;
            
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
                if (e.RoutingKey.StartsWith("r.oe.bm.engine."))
                {
                    switch (messageType)
                    {
                        case "OnOddsChange":
                            var oddschange = JsonSerializer.Deserialize<OnExternalOddsChangeMessage>(payload);
                            await _boHub.Clients.All.SendAsync("onoddschange", oddschange);
                            break;
                        case "OnBetStopFixture":
                            var betStopFixture = JsonSerializer.Deserialize<OnExternalBetStopFixtureMessage>(payload);
                            await _boHub.Clients.All.SendAsync("onbetstopfixture", betStopFixture);
                            break;
                        case "OnBetStopGlobal":
                            var betStopGlobal = JsonSerializer.Deserialize<OnExternalBetStopGlobalMessage>(payload);
                            await _boHub.Clients.All.SendAsync("onbetstopglobal", betStopGlobal);
                            break;
                        case "OnSignIn":
                            var signin = JsonSerializer.Deserialize<OnExternalOddsChangeMessage>(payload);
                            await _boHub.Clients.All.SendAsync("onsignin", signin);
                            break;
                        case "OnNewNotification":
                            var notificationsModel = JsonSerializer.Deserialize<NotificationsModel>(payload);
                            await _boHub.Clients.All.SendAsync("onnewnotification", notificationsModel);
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
