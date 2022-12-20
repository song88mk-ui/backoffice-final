using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class MessageRequest
    {
        public string Payload { get; set; }
        public string MessageType { get; set; }
        public string RoutingKey { get; set; }
        public string CorrelationId { get; set; }
    }
}
