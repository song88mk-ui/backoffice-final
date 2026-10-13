namespace Backoffice.Shared.Models.Requests
{
    public class ClientUpdateRowRequest
    {
        public int RowNumber { get; set; }

        // The Platform client ID (Platform.dbo.Client.ClientId), as typed. Identifies the client being updated.
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string Website { get; set; }
        public string Region { get; set; }
        public string Email { get; set; }
        // Null = blank cell, preserve the client's existing value.
        public decimal? MonthlyFee { get; set; }
        public decimal? RevenueShareSportsbook { get; set; }
        public decimal? RevenueShareJackpot { get; set; }
        public string LimitId { get; set; }
        public string Endpoint { get; set; }

        public string CurrencyCode { get; set; }

        // Blank = create-or-update this currency. "Remove" = delete this currency.
        // A currency not present as a row at all is left untouched.
        public string CurrencyAction { get; set; }

        // Null = blank cell. Every bet column is required on a non-Remove currency row.
        public decimal? SoccerMinPerTicket { get; set; }
        public decimal? SoccerMaxPerTicket { get; set; }
        public decimal? SoccerMaxPerGame { get; set; }
        public decimal? AllSportsMinPerTicket { get; set; }
        public decimal? AllSportsMaxPerTicket { get; set; }
        public decimal? AllSportsMaxPerGame { get; set; }
        public decimal? ParlayMinPerTicket { get; set; }
        public decimal? ParlayMaxPerTicket { get; set; }
        public decimal? ParlayMaxPayoutPerTicket { get; set; }
        public decimal? ParlayMaxWinningsBonus { get; set; }
    }
}
