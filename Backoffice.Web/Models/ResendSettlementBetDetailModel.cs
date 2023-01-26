using static Backoffice.Shared.Models.Response.ResendSettlementBetDetailResponse;

namespace Backoffice.Web.Models
{
    public class ResendSettlementBetDetailModel
    {
        public ResendSettlementBetDetailModel()
        {
            Bet = new ResendSettlementBetDetail();
        }
        public ResendSettlementBetDetail Bet { get; set; }
    }
}
