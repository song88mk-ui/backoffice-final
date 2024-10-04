using Backoffice.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class AddOrUpdateAnnouncementRequest
    {
        public int Id { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public List<string> ClientCodes { get; set; }
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Message { get; set; }
        public bool IsActive { get; set; }
    }
}