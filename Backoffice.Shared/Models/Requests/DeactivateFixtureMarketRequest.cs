
using Backoffice.Shared.Enums;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class DeactivateFixtureMarketRequest
    {
        public DeactivateFixtureMarketRequest()
        {
            Markets = new List<MarketMessage>();
        }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public string ProviderFixtureId { get; set; }
        public string Raw { get; set; }
        public List<MarketMessage> Markets { get; set; }

        public class MarketMessage
        {
            public MarketMessage()
            {
                Selections = new List<SelectionMessage>();
            }
            public string MarketTemplateId { get; set; }
            public string MarketTemplateName { get; set; }
            public string MarketName { get; set; }
            public string Line { get; set; }
            public int MarketStatusId { get; set; }
            public string MarketSpecifier { get; set; }
            public List<SelectionMessage> Selections { get; set; }
        }

        public class SelectionMessage
        {
            public string SelectionTemplateId { get; set; }
            public string SelectionName { get; set; }
            public bool IsActive { get; set; }
            public string SelectionSpecifier { get; set; }

        }
    }
}
