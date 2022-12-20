using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class NotificationResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime DateCreated { get; set; }
        public string Page { get; set; }
        public string ClientCode { get; set; }
        public string ClaimsType { get; set; }
        public int? SportsId { get; set; }
        public string Details { get; set; }
        public string PageRedirect { get; set; }
        public bool IsRead { get; set; }
    }
}
