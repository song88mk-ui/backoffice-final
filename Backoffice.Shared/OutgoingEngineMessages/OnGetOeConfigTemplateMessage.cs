using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.OutgoingEngineMessages
{
    public class OnGetOeConfigTemplateMessage
    {
        public string ConnectionId { get; set; } = "";
        public string UserId { get; set; }
        public string ConfigTemplateId { get; set; }
        public string Filename { get; set; }
        public string AccessToken { get; set; }
    }
}
