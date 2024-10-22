using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models
{
    public class EventModel
    {
        public EventModel()
        {
            Events = new List<Event>();
        }
        public List<Event> Events { get; set; }
    }

    public class Event
    {
        public string Action { get; set; }
        public DateTime TimestampStarted { get; set; }
        public DateTime TimestampFinished { get; set; }
        public long ProcessingTime { get; set; }
    }
}
