using Backoffice.Shared.Data.Trading.SQLEntities;
using Backoffice.Shared.Models;
using System.Collections.Generic;

namespace Backoffice.Shared.OutgoingEngineMessages
{
    public class OnBulkUpdateConfigMessage
    {
        public OnBulkUpdateConfigMessage()
        {
            ClientLeagueConfigs = new List<ClientLeagueConfig>();
            ClientMarketConfigs = new List<ClientMarketConfigModel>();
        }

        public string UserId { get; set; }
        public string FileName { get; set; }
        public int SportId { get; set; }
        public bool IsPublish { get; set; }
        public bool IsLastMessage { get; set; }
        public List<ClientLeagueConfig> ClientLeagueConfigs { get; set; }
        public List<ClientMarketConfigModel> ClientMarketConfigs { get; set; }
    }
}
