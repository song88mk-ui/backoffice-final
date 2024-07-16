using Backoffice.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.OutgoingEngineMessages
{
    public class OnUpdateClientMarketConfigMessage
    {
        public List<ClientMarketConfigModel2> ConfigSettings { get; set; } = new List<ClientMarketConfigModel2>();
    }
}
