namespace Backoffice.Shared.Models.Requests
{
    public class ClientImportRowRequest
    {
        public int RowNumber { get; set; }

        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string Website { get; set; }
        public string Region { get; set; }
        public string Email { get; set; }
        public decimal MonthlyFee { get; set; }
        public decimal RevenueShareSportsbook { get; set; }
        public decimal RevenueShareJackpot { get; set; }
        public string LimitId { get; set; }
        public string Endpoint { get; set; }

        public string ReferenceClientCode { get; set; }

        public string CurrencyCode { get; set; }
        public decimal SoccerMinPerTicket { get; set; }
        public decimal SoccerMaxPerTicket { get; set; }
        public decimal SoccerMaxPerGame { get; set; }
        public decimal AllSportsMinPerTicket { get; set; }
        public decimal AllSportsMaxPerTicket { get; set; }
        public decimal AllSportsMaxPerGame { get; set; }
        public decimal ParlayMinPerTicket { get; set; }
        public decimal ParlayMaxPerTicket { get; set; }
        public decimal ParlayMaxPayoutPerTicket { get; set; }
        public decimal ParlayMaxWinningsBonus { get; set; }
    }
}
