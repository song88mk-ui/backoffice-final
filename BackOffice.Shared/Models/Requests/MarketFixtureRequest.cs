using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class MarketFixtureRequest
    {
        public int FixtureTypeId { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public string MarketTemplateId { get; set; }
        public bool AllFixtureStatus { get; set; } = false;
    }
}
