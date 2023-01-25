using Backoffice.Shared.OutgoingEngineMessages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class MessageSettlementRequest
    {
        public MessageSettlementIodocsRequest MessageSettlementIodocsRequest { get; set; }
        public OnActivityLogMessage OnActivityLogMessage { get; set; }
    }

    public class MessageSettlementIodocsRequest
    {
        public string Product { get; set; }
        public string FixtureSpecifier { get; set; }
        public int RequestId { get; set; }
        public int NodeId { get; set; }
    }
    public class MessageSettlementTo
    {
        public string BetId { get; set; }
        public DateTime BetDate { get; set; }
        public string BetSelectionId { get; set; }
        public string SportName { get; set; }
        public string FixtureName { get; set; }
        public string Reason { get; set; }
        public string SelectionSpecifier { get; set; }
        public string FixtureSpecifier { get; set; }
        public int RequestId { get; set; }
        public int NodeId { get; set; }
        public int ProductId { get; set; }
        public string MarketTemplateId { get; set; }
        public int InitialBetSelectionStatusId { get; set; }
    }
}
