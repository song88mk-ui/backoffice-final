using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.OutgoingWebSocketMessages
{
    public class OutgoingBaseMessage
    {
        public List<string> Gr { get; set; } = new List<string>();
    }
}
