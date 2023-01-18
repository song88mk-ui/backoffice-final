using System.Collections.Generic;

namespace Backoffice.Shared.Utilities
{
    public class SportsLiveTimeline
    {
        public int SportId { get; set; }

        public int Overtime { get; set; }

        public List<int> Time { get; set; }
    }
}