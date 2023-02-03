using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.OutgoingEngineMessages
{
    public class OnExportConfigMarginMessage
    {
        public string SportIds { get; set; }
        public string ClientCode { get; set; }
        public string FileName { get; set; }
        public string UserId { get; set; }
    }
}
