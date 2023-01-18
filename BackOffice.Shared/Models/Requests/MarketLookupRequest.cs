using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class MarketLookupRequest
    {
        public int FixtureTypeId { get; set; }
        public int SportId { get; set; }
    }
}
