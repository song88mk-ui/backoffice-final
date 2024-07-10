using Backoffice.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.OutgoingEngineMessages
{
    public class OnMarketConfigMessage
    {
        public OnMarketConfigMessage()
        {
            Markets = new List<MarketConfig>();
        }
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public List<MarketConfig> Markets { get; set; }
    }
}
