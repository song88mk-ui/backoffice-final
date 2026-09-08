namespace Backoffice.Web.Models
{
    public class CancelBetHistoryCallbackModel
    {
        public string CancelledBy { get; set; }
        public string BetReferenceId { get; set; }
        public string MemberName { get; set; }
        public string ClientName { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
    }
}
