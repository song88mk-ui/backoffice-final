using System;

namespace Backoffice.Shared.Models
{
    public class NotificationsModel
    {
        public Guid Id { get; set; }
        public string Timestamp { get; set; }
        public string Page { get; set; }
        public string Details { get; set; }
        public string PageRedirect { get; set; }
        public bool IsRead { get; set; }
        public string UserId { get; set; }
        public string DateText { get; set; }
        public string ClaimsType { get; set; }
        public string ClientCode { get; set; }
    }
}
