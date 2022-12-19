using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class PriceBoostConfigRequest
    {
        public string ClientCode { get; set; }
        public string SportId { get; set; }
        public string SegmentId { get; set; }
        public string LeagueId { get; set; }
        public int FixtureTypeId { get; set; }
        public string ProviderFixtureId { get; set; }
        public string MarketTemplateId { get; set; }
        public string SelectionTemplateId { get; set; }
        public string SelectionSpecifier { get; set; }
    }
}
