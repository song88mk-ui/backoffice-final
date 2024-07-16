using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.OutgoingEngineMessages
{
    public class OnLeagueConfigMessage
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public bool IsActive { get; set; }
    }
}
