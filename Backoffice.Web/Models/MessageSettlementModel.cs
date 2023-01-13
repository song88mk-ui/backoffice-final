namespace Backoffice.Web.Models
{
    public class MessageSettlementModel
    {
        public long Id { get; set; }
        public DateTime BetDate { get; set; }
        public string BetId { get; set; }
        public string Sports { get; set; }
        public string MessagesSettled { get; set; }
        public string Product { get; set; }
        public string SettledBy { get; set; }
        public DateTime DateSettled { get; set; }
        public string Reason { get; set; }
    }
}
