using Backoffice.Shared.Enums;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ManualMarketSettlementRequest
    {
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
        public int FixtureStatusId { get; set; }
        public string FixtureStatusName { get; set; }
        public string FixtureSpecifier { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public string MarketTemplateId { get; set; }
        public string MarketId { get; set; }
        public string MarketName { get; set; }
        public string MarketSpecifier { get; set; }
        public string SelectionTemplateId { get; set; }
        public string SelectionId { get; set; }
        public string SelectionName { get; set; }
        public string SelectionSpecifier { get; set; }
        public BetStatus SelectionStatus { get; set; }
        public string Line { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Odds { get; set; }
        public string ProviderFixtureId { get; set; }
        public string SettlementScore { get; set; }
        public decimal DeadHeatFactor { get; set; } = 1;
        public double Probability { get; set; }
        public double WonProbability { get; set; }
        public double LostProbability { get; set; }
        public double VoidProbability { get; set; }
        public double HalfWonProbability { get; set; }
        public double HalfLostProbability { get; set; }
        public string CorrelationId { get; set; }
        public string Reason { get; set; }
        public string ManualSettledBy { get; set; }
    }
}
