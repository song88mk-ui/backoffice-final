using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ClientConfigRequest
    {
        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public int FixtureTypeId { get; set; }
        public int IsActive { get; set; }
    }
}
