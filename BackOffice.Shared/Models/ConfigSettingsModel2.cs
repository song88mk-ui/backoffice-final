using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class ClientMarketConfigModel2
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public string MarketTemplateId { get; set; }
        public int FixtureTypeId { get; set; }
        public bool IsActive { get; set; }
        public string Prematch { get; set; }
        public string Live { get; set; }
    }
    public class ClientLeagueConfigModel2
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public bool IsActive { get; set; }
    }
}
