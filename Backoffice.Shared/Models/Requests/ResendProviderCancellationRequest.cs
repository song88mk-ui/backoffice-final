using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    // BO-125: bets whose Cancelled status must be sent to the provider (Bet Genius / GTS) again. Only the
    // ids are sent. The Platform API reads each bet's status itself and refuses any bet that is not Cancelled.
    public class ResendProviderCancellationRequest
    {
        public ResendProviderCancellationRequest()
        {
            BetIds = new List<string>();
        }

        public List<string> BetIds { get; set; }
    }
}
