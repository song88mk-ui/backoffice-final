using Backoffice.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class AddOrUpdateAnnouncementRequest
    {
        public int Id { get; set; }
        public List<string> ClientCodes { get; set; }
        public List<AnnouncementMessage> AnnouncementMessages { get; set; }
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public string UserId { get; set; }
        public string UserRoleId { get; set; }
    }
    public class AnnouncementMessage
    {
        public LanguageCode LanguageCode { get; set; }
        public string Message { get; set; }
    }
}