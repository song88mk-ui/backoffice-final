using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class ManualMessageSettlementAuditResponse
    {
        public ManualMessageSettlementAuditResponse()
        {
            ManualMessageSettlementAudit = new List<ManualMessageSettlementAudit>();
        }
        public List<ManualMessageSettlementAudit> ManualMessageSettlementAudit { get; set; }
        public MetaData MetaData { get; set; }
    }

    public class ManualMessageSettlementAudit
    {
        public long Id { get; set; }
        public Guid BetId { get; set; }
        public DateTime BetDate { get; set; }
        public Guid BetSelectionId { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public long FixtureId { get; set; }
        public string FixtureName { get; set; }
        public int MarketTemplateId { get; set; }
        public int InitialBetSelectionStatusId { get; set; }
        public int ProductId { get; set; }
        public string SelectionSpecifier { get; set; }
        public string FixtureSpecifier { get; set; }
        public int RequestId { get; set; }
        public int? NodeId { get; set; }
        public string Reason { get; set; }
        public string Response { get; set; }
        public string SettledBy { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
