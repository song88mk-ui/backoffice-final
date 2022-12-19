using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class NotificationsDetailsModel
    {
        public Guid Id { get; set; }
        public string Page { get; set; }
        public string ClientCode { get; set; }
        public int? SportsId { get; set; }
        public string Details { get; set; }
        public string PageRedirect { get; set; }
        public Guid? UserId { get; set; }
        public string ClaimType { get; set; }
        public int Type { get; set; }
    }
}
