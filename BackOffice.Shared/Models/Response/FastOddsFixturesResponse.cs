using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class FastOddsFixturesResponse
    {
        public FastOddsFixturesResponse()
        {
            Fixtures = new List<Fixture>();
        }

        public List<Fixture> Fixtures { get; set; }

        #region Nested Class

        public class Fixture
        {
            public Fixture()
            {
                Markets = new List<Market>();
            }

            public int SportId { get; set; }
            public string SportName { get; set; }
            public int SegmentId { get; set; }
            public string SegmentName { get; set; }
            public int LeagueId { get; set; }
            public string LeagueName { get; set; }
            public string ProviderFixtureId { get; set; }
            public string FixtureName { get; set; }
            public DateTime FixtureStartDate { get; set; }
            public int FixtureStatusId { get; set; }
            public string FixtureStatusName { get; set; }
            public string Score { get; set; }
            public int FixtureTypeId { get; set; }
            public List<Market> Markets { get; set; }
        }

        public class Market
        {
            public Market()
            {
                Selections = new List<Selection>();
            }

            public string ProviderMarketId { get; set; }
            public string ProviderFixtureId { get; set; }
            public string MarketName { get; set; }
            public int MarketStatusId { get; set; }
            public string MarketStatusName { get; set; }
            public string MarketTemplateId { get; set; }
            public string MarketSpecifier { get; set; }
            public string Line { get; set; }
            public decimal BPMargin { get; set; }
            public List<Selection> Selections { get; set; }
        }

        public class Selection
        {
            public Selection()
            {
                BookmakerOdds = new List<BookmakerOdds>();
            }

            public string ProviderSelectionId { get; set; }
            public string ProviderMarketId { get; set; }
            public string ProviderFixtureId { get; set; }
            public string SelectionName { get; set; }
            public string SelectionTemplateId { get; set; }
            public string SelectionSpecifier { get; set; }
            public string CorrelationId { get; set; }
            public List<BookmakerOdds> BookmakerOdds { get; set; }
            public int SortOrder { get; set; }
        }

        public class BookmakerOdds
        {
            public string BookmakerId { get; set; }
            public decimal Odds { get; set; }
            public bool IsActive { get; set; }
            public bool IsHighest { get; set; }
            public bool IsLowest { get; set; }
        }

        #endregion
    }
}
