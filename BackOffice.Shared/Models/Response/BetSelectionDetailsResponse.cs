using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class BetSelectionDetailsResponse
    {
        public BetSelectionDetailsResponse()
        {
            Selections = new List<BetSelectionDetails>();
        }
        public List<BetSelectionDetails> Selections { get; set; }

        #region Nested Class
        public class BetSelectionDetails
        {
            public Guid BetId { get; set; }
            public Guid BetSelectionId { get; set; }
            public int SportId { get; set; }
            public string SportName { get; set; }
            public int SegmentId { get; set; }
            public string SegmentName { get; set; }
            public int LeagueId { get; set; }
            public string LeagueName { get; set; }
            public string FixtureId { get; set; }
            public string FixtureName { get; set; }
            public int FixtureTypeId { get; set; }
            public string FixtureTypeName { get; set; }
            public string MarketTemplateId { get; set; }
            public string MarketId { get; set; }
            public string MarketName { get; set; }
            public string SelectionTemplateId { get; set; }
            public string SelectionId { get; set; }
            public string SelectionName { get; set; }
            public int FixtureStatusId { get; set; }
            public string FixtureStatusName { get; set; }
            public string Line { get; set; }
            public decimal Odds { get; set; }
            public int BetSelectionStatusId { get; set; }
            public string BetSelectionStatusName { get; set; }
            public bool IsSettled { get; set; }
            public bool IsResettled { get; set; }
            public string FixtureSpecifier { get; set; }
            public string SelectionSpecifier { get; set; }
            public DateTime FixtureStartDate { get; set; }
            public string BetScore { get; set; }
            public string SettlementScore { get; set; }
            public double Probability { get; set; }
            public double WonProbability { get; set; }
            public double LostProbability { get; set; }
            public double VoidProbability { get; set; }
            public double HalfWonProbability { get; set; }
            public double HalfLostProbability { get; set; }
            public string CorrelationId { get; set; }
            public decimal DeadHeatFactor { get; set; }
            public string UserId { get; set; }
        }
        #endregion
    }
}
