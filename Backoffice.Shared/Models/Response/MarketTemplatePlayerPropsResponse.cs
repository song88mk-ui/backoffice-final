using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class MarketTemplatePlayerPropsResponse
    {
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public string MarketTemplates { get; set; }
    }
}
