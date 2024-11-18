using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;
using Backoffice.Shared.OutgoingWebSocketMessages;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.WebSocket.Service.Services
{
    public class SportsbookService : ISportsbookService
    {

        private readonly ILogger<SportsbookService> _logger;

        private readonly string HubPublishOddsChange;

        public SportsbookService(ILogger<SportsbookService> logger)
        {
            _logger = logger;

            HubPublishOddsChange = "PublishOddsChange";
        }

        public async Task ProcessOnMonitoringOddsChange(OnExternalOddsChangeMessage message, HubConnection hubConnection)
        {
            var payload = new OutgoingOddsChange
            {
                Gr = GetGroupForMonitoringMarkets(message),
                Pa = message
            };

            if (message.SportId == 137)
                Console.WriteLine($"OddsChange - {message.SportName,-20} - {message.LeagueId,-10} - {message.LeagueName,-60} - {message.CorrelationId}");
            await PublishToWebSocket(HubPublishOddsChange, hubConnection, payload);
        }
        public async Task ProcessOnMonitoringDetailsOddsChange(OnExternalOddsChangeMessage message, HubConnection hubConnection)
        {
            var payload = new OutgoingOddsChange
            {
                Gr = GetGroupForMonitoringDetailsMarkets(message),
                Pa = message
            };
            await PublishToWebSocket(HubPublishOddsChange, hubConnection, payload);
        }
        public async Task ProcessNotification(NotificationsDetailsModel message, HubConnection hubConnection)
        {

            var date = DateTime.UtcNow.ToString();
            var payload = new NotificationsModel
            {
                Id = message.Id,
                Timestamp = date,
                Page = message.Page,
                Details = message.Details,
                PageRedirect = message.PageRedirect,
                IsRead = false,
                UserId = message.UserId?.ToString(),
                DateText = date,
                ClaimsType = message.ClaimType,
                ClientCode = message.ClientCode,
            };


            await PublishToWebSocket("Notification", hubConnection, payload);
        }

        private static async Task PublishToWebSocket<T>(string method, HubConnection hubConnection, T payload)
        {
            try
            {
                await hubConnection.SendAsync(method, payload);
            }
            catch (Exception)
            {
            }
        }

        private List<string> GetGroupForMonitoringMarkets(OnExternalOddsChangeMessage message)
        {
            var result = new List<string>();

            //result.Add($"sports:{message.SportId}:segments:{message.SegmentId}:leagues:{message.LeagueId}:fixtures");
            result.Add($"leagues:{message.LeagueId}:fixtures");

            return result;
        }

        private List<string> GetGroupForMonitoringDetailsMarkets(OnExternalOddsChangeMessage message)
        {
            var result = new List<string>();

            result.Add($"fixturedetails:{message.FixtureId}");

            return result;
        }
    }
}
