using Backoffice.Shared.Data.Trading.SQLEntities;
using System.Collections.Generic;

namespace Backoffice.Shared.OutgoingEngineMessages
{
    public class OnCopyClientSettingsMessage
    {
        public string UserId { get; set; }
        public string FromClientCode { get; set; }
        public string ToClientCode { get; set; }
        public List<ClientLeagueConfig> ToRemoveConfig { get; set; }
    }
}
