using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.IncomingSbEngineMessages
{
    public class IncomingBetStopGlobalMessage
    {
        public bool IsBetStop { get; set; }
        public string CorrelationId { get; set; }
    }
}
