using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class SportsLiveTimelineModel
    {
        public int SportId { get; set; }

        public int Overtime { get; set; }

        public List<int> Time { get; set; }
    }
}
