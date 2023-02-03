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
            Sports = new List<Sport>();
            Currencies = new List<Currency>();
            FromBetStatuses = new List<BetStatus>();
            ToBetStatuses = new List<BetStatus>();
        }
        #endregion        

        public List<Client> Clients { get; set; }
        public List<Sport> Sports { get; set; }
        public List<Currency> Currencies { get; set; }
        public List<BetStatus> FromBetStatuses { get; set; }
        public List<BetStatus> ToBetStatuses { get; set; }

        public DateTime DateFrom { get; set; } = DateTime.Now.AddDays(-7).Date;
        public DateTime DateTo { get; set; } = DateTime.Now.Date;

        public string BetId { get; set; } = "";
        public string MemberName { get; set; } = "";
        public string SettledBy { get; set; } = "";

        #region Nested Class
        public class Client
        {
            public long ClientId { get; set; }
            public string ClientCode { get; set; }
            public string ClientName { get; set; }
        }
        public class Sport
        {
            public long SportId { get; set; }
            public string SportName { get; set; }
        }

        public class Currency
        {
            public int CurrencyId { get; set; }
            public string CurrencyCode { get; set; }
        }

        public class BetStatus
        {
            public int BetStatusId { get; set; }
            public string BetStatusName { get; set; }
        }
        #endregion
    }
}
