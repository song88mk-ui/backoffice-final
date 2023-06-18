using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class SportsTimeSettings
    {
        public int SportsId { get; set; }
        public string SportName { get; set; }
        public int MarketId { get; set; }
        public string MarketName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public PrematchSettings PrematchSettings { get; set; }
        public LiveMatchSettings LiveMatchSettings { get; set; }
    }

    public class PrematchSettings
    {
        public int Start { get; set; }
        public int End { get; set; }
        public int Interval { get; set; }

        public OddsKey OddsKey { get; set; }
    }

    public class LiveMatchSettings
    {
        public int Start { get; set; }
        public int End { get; set; }
        public int Interval { get; set; }
        public OddsKey OddsKey { get; set; }
    }

    public class OddsKey
    {
        public int FirstKey { get; set; }
        public int FirstKeyLocation { get; set; }
        public int LastKey { get; set; }
        public int LastKeyLocation { get; set; }
    }
}
