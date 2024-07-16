using Backoffice.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

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
