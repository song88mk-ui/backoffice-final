using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.OutgoingEngineMessages
{
    public class OnActivityLogListMessage
    {
        public OnActivityLogListMessage()
        {
            OnActivityLogMessages = new List<OnActivityLogMessage>();
        }
        public List<OnActivityLogMessage> OnActivityLogMessages { get; set; }
    }
    public class OnActivityLogMessage
    {
        public long Id { get; set; }
        public string Page { get; set; }
        public string Payload { get; set; }
        public string Activity { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string UserId { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
