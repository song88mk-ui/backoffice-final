using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class LogInActivityLogModel
    {
        public string UserId { get; set; }
        public string Token { get; set; }
        public string UserAgent { get; set; }
        public string IpAddress { get; set; }
        public string Country { get; set; }
        public DateTime Date { get; set; }
    }
}
