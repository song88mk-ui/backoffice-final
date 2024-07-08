using Backoffice.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.OutgoingEngineMessages
{
    public class OnUpdateClientLeagueConfigMessage
    {
        public List<ClientLeagueConfigModel2> ConfigSettings { get; set; } = new List<ClientLeagueConfigModel2>();
    }
}
