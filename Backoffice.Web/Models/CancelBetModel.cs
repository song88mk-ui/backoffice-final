namespace Backoffice.Web.Models
{
    public class CancelBetReferenceModel
    {
        public CancelBetReferenceModel()
        {
            BetReferenceIds = new List<string>();
        }
        public string Reason { get; set; }
        public List<string> BetReferenceIds { get; set; }
    }
}
