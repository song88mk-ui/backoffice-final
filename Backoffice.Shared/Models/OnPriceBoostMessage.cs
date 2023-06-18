using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class OnPriceBoostMessage
    {
        public OnPriceBoostMessage()
        {
            ClientBoostedSelectionNameTranslations = new List<ClientBoostedSelectionTranslationMessage>();
            ClientBoostedSelectionNames = new List<ClientBoostedSelectionNameMessage>();
        }
        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public int FixtureTypeId { get; set; }
        public string ProviderFixtureId { get; set; }
        public string MarketTemplateId { get; set; }
        public string SelectionTemplateId { get; set; }
        public string SelectionSpecifier { get; set; }
        public decimal BoostedOdds { get; set; }
        public bool IsBoosted { get; set; }
        public string ClientBoosted { get; set; }
        public string ClientMarketBoosted { get; set; }
        public List<ClientBoostedSelectionNameMessage> ClientBoostedSelectionNames { get; set; }
        public List<ClientBoostedSelectionTranslationMessage> ClientBoostedSelectionNameTranslations { get; set; }
    }
    public class ClientBoostedSelectionNameMessage
    {
        public string ClientCode { get; set; }
        public string BoostedSelectionName { get; set; }
    }
    public class ClientBoostedSelectionTranslationMessage
    {
        public string LanguageCode { get; set; }
        public string ClientCode { get; set; }
        public string BoostedSelectionName { get; set; }
    }
}
