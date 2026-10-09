using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class CopyClientOddstableLeagueSettingRequest
    {
        public string SourceClientCode { get; set; }
        public List<string> TargetClientCodes { get; set; } = new List<string>();
        public int SportId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
