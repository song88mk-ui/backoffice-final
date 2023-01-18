using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class MarketConfig
    {
        public int FixtureTypeId { get; set; }
        public string MarketTemplateId { get; set; }
        public bool IsActive { get; set; }
        public string Prematch { get; set; }
        public string Live { get; set; }
    }
}
