using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class LogInActivityLogResponse
    {
        public LogInActivityLogResponse()
        {
            ActivityLogs = new List<LogInActivityLogModel>();
        }
        public List<LogInActivityLogModel> ActivityLogs { get; set; }
        public MetaData MetaData { get; set; }
    }
}
