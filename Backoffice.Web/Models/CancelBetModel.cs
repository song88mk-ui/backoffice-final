namespace Backoffice.Web.Models
{
    public class CancelBetModel
    {
        public CancelBetModel()
        {
            BetReferenceIds = new List<BetReferenceModel>();
        }
        public string Reason { get; set; }
        public List<BetReferenceModel> BetReferenceIds { get; set; }

        public class BetReferenceModel
        {
            public int Num { get; set; }
            public string BetReferenceId { get; set; }
        }
    }
}
