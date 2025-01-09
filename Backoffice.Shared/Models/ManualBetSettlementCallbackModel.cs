using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class ManualBetSettlementCallbackModel
    {
        #region Ctor
        public ManualBetSettlementCallbackModel()
        {
            Sports = new List<SettlementSport>();
            Currencies = new List<SettlementCurrency>();
            FromBetStatuses = new List<SettlementBetStatus>();
            ToBetStatuses = new List<SettlementBetStatus>();
        }
        #endregion        

        public List<SettlementClient> Clients { get; set; }
        public List<SettlementSport> Sports { get; set; }
        public List<SettlementCurrency> Currencies { get; set; }
        public List<SettlementBetStatus> FromBetStatuses { get; set; }
        public List<SettlementBetStatus> ToBetStatuses { get; set; }

        public DateTime DateFrom { get; set; } = DateTime.UtcNow.AddDays(-7).Date;
        public DateTime DateTo { get; set; } = DateTime.UtcNow.Date;

        public string BetId { get; set; } = "";
        public string MemberName { get; set; } = "";
        public string SettledBy { get; set; } = "";

        #region Nested Class
        public class SettlementClient
        {
            public long ClientId { get; set; }
            public string ClientCode { get; set; }
            public string ClientName { get; set; }
        }
        public class SettlementSport
        {
            public long SportId { get; set; }
            public string SportName { get; set; }
        }

        public class SettlementCurrency
        {
            public int CurrencyId { get; set; }
            public string CurrencyCode { get; set; }
        }

        public class SettlementBetStatus
        {
            public int BetStatusId { get; set; }
            public string BetStatusName { get; set; }
        }
        #endregion
    }
}
