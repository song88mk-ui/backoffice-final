using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models
{
    public class ConfigTemplateMarginModel
    {
        public ConfigTemplateMarginModel()
        {
            ConfigTemplateMargins = new List<ConfigTemplateMargin>();
        }
        public List<ConfigTemplateMargin> ConfigTemplateMargins { get; set; }
    }

    public class ConfigTemplateMargin
    {
        public long ConfigTemplateId { get; set; }
        public int SportId { get; set; }
        public string MarketTemplateId { get; set; }
        public int FixtureTypeId { get; set; }
        public bool IsActive { get; set; }
        public string Prematch { get; set; }
        public string Live { get; set; }
    }
}
