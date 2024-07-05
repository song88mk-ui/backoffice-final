using Backoffice.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.OutgoingEngineMessages
{
    public class OutgoingUpdateClientMarketConfigMessage
    {
        public List<ConfigSettingsModel2> ConfigSettings { get; set; } = new List<ConfigSettingsModel2>();
    }
}
