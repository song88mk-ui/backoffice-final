using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class ClientMarketConfigModel
    {
        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public string MarketTemplateId { get; set; }
        public string MarketTemplateName { get; set; }
        public int FixtureTypeId { get; set; }
        public Guid ClientId { get; set; }
        public bool IsActive { get; set; }
        public string Prematch { get; set; }
        public string Live { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }
    }
}
