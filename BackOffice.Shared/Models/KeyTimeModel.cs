using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class KeyTimeModel
    {
        public int SortOrder { get; set; }
        public int Time { get; set; }
        public string TimeDisplay { get; set; }
        public int? Key { get; set; }
    }
}
