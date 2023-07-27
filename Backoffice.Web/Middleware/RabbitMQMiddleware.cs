using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client.Events;
using Serilog;
using System.Drawing;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Backoffice.Web.Middleware
{
    public class RabbitMQMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMessageBroker _messageBrokerHk;
        //private readonly IMessageBroker _messageBrokerSa;
        private readonly IHubContext<BoHub> _boHub;

        public RabbitMQMiddleware(RequestDelegate next, IHubContext<BoHub> boHub, IMessageBroker messageBroker, IConfiguration config)
        {
            _next = next;
            _messageBrokerHk = messageBroker;
            //_messageBrokerSa = messageBroker;
            _boHub = boHub;

            _messageBrokerHk.Connect(config.GetSection($"MessageBroker:HK").Get<MessageBrokerCredentialModel>());
            //_messageBrokerSa.Connect(config.GetSection($"MessageBroker:SA").Get<MessageBrokerCredentialModel>());

            _messageBrokerHk.MessageReceived += async (sender, e) =>
            {
                var payload = Encoding.UTF8.GetString(e.Body);
                var messageType = e.BasicProperties.Type;

                if (e.RoutingKey.StartsWith("r.oe.bm.engine."))
                {
                    await SignalRSendMessage(messageType, payload, "HK");
                }
            };
            //_messageBrokerHk.MessageReceived += async (sender, e) =>
            //{
            //    var payload = Encoding.UTF8.GetString(e.Body);
            //    var messageType = e.BasicProperties.Type;

            //    if (e.RoutingKey.StartsWith("r.oe.bm.engine."))
            //    {
            //        await SignalRSendMessage(messageType, payload, "SA");
            //    }
            //};
        }

        public async Task Invoke(HttpContext httpContext)
        {
            await _next(httpContext);
        }

        private async Task SignalRSendMessage(string messageType, string payload, string group = "")
        {
            try
            {
                var hubClients = string.IsNullOrWhiteSpace("") ? _boHub.Clients.All : _boHub.Clients.Group(group.ToUpper());

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
                        var notificationsModel = JsonSerializer.Deserialize<NotificationsDetailsModel>(payload);
                        var date = DateTime.UtcNow.ToString();
                        var notifPayload = new NotificationsModel
                        {
                            Id = notificationsModel.Id,
                            Timestamp = date,
                            Page = notificationsModel.Page,
                            Details = notificationsModel.Details,
                            PageRedirect = notificationsModel.PageRedirect,
                            IsRead = false,
                            UserId = notificationsModel.UserId.ToString(),
                            DateText = date,
                            ClaimsType = notificationsModel.ClaimType,
                            ClientCode = notificationsModel.ClientCode,
                        };
                        hubClients.SendAsync("Notification", notifPayload).Wait();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }
        }
    }
}
