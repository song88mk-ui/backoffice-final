using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class ConfigLogsResponse
    {
        public int Id { get; set; }
        public DateTime TimeStamp { get; set; }
        public string ClientCode { get; set; }
        public string Sports { get; set; }
        public string Segment { get; set; }
        public string League { get; set; }
        public string MarketTemplate { get; set; }
        public string OldPrematchSettings { get; set; }
        public string NewPrematchSettings { get; set; }
        public string OldLiveSettings { get; set; }
        public string NewLiveSettings { get; set; }
        public bool? OldStatus { get; set; }
        public bool? NewStatus { get; set; }
        public string Username { get; set; }
        public bool IsBulkUpload { get; set; }
        public MetaData MetaData { get; set; }
    }
}
