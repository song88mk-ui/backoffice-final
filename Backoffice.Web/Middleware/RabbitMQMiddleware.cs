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
        private readonly List<IMessageBroker> _messageBroker;
        private readonly IHubContext<BoHub> _boHub;

        public RabbitMQMiddleware(RequestDelegate next, IHubContext<BoHub> boHub, IConfiguration config)
        {
            _next = next;
            _messageBroker = new List<IMessageBroker>();
            _boHub = boHub;

            var regions = new[] { "", "HK", "SA" };

            foreach(var item in regions)
            {
                var messageBroker = new MessageBroker(config);
                if (string.IsNullOrWhiteSpace(item))
                {
                    messageBroker.Connect(config?.GetSection($"MessageBroker:{item}").Get<MessageBrokerCredentialModel>());
                }
                else
                {
                    messageBroker.Connect();
                }

                messageBroker.MessageReceived += (sender, e) =>
                {
                    var payload = Encoding.UTF8.GetString(e.Body);
                    var messageType = e.BasicProperties.Type;

                    try
                    {
                        var hubClients = string.IsNullOrWhiteSpace(item) ? _boHub.Clients.All : _boHub.Clients.Group(item);

                        if (e.RoutingKey.StartsWith("r.oe.bm.engine."))
                        {
                            switch (messageType)
                            {
                                case "OnOddsChange":
                                    var oddschange = JsonSerializer.Deserialize<OnExternalOddsChangeMessage>(payload);
                                    hubClients.SendAsync("onoddschange", oddschange).Wait();
                                    break;
                                case "OnBetStopFixture":
                                    var betStopFixture = JsonSerializer.Deserialize<OnExternalBetStopFixtureMessage>(payload);
                                    hubClients.SendAsync("onbetstopfixture", betStopFixture).Wait();
                                    break;
                                case "OnBetStopGlobal":
                                    var betStopGlobal = JsonSerializer.Deserialize<OnExternalBetStopGlobalMessage>(payload);
                                    hubClients.SendAsync("onbetstopglobal", betStopGlobal).Wait();
                                    break;
                                case "OnSignIn":
                                    var signin = JsonSerializer.Deserialize<OnExternalOddsChangeMessage>(payload);
                                    hubClients.SendAsync("onsignin", signin).Wait();
                                    break;
                                case "OnNewNotification":
                                    var notificationsModel = JsonSerializer.Deserialize<NotificationsModel>(payload);
                                    hubClients.SendAsync("onnewnotification", notificationsModel).Wait();
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                };
                _messageBroker.Add(messageBroker);
            }

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
                var hubClients = string.IsNullOrWhiteSpace("") ? _boHub.Clients.All : _boHub.Clients.Group("");

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
