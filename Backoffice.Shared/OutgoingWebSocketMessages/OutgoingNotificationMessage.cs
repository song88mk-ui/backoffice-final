using Backoffice.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.OutgoingWebSocketMessages
{
    public class OutgoingNotificationMessage : OutgoingBaseMessage
    {
        public string UsId { get; set; }
        public NotificationsModel Pa { get; set; }
    }
}
