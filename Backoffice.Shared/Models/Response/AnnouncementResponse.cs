using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class AnnouncementResponse
    {
        public AnnouncementResponse()
        {
            Announcements = new List<AnnouncementDTO>();
        }

        public List<AnnouncementDTO> Announcements { get; set; }

        #region Nested Class

        public class AnnouncementDTO
        {
            public AnnouncementDTO()
            {
                Clients = new List<ClientInfo>();
                Languages = new List<AnnouncementMessage>();
            }
            public int Id { get; set; }
            public bool IsActive { get; set; }
            public string Title { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Message { get; set; }
            public LanguageCode LanguageCode { get; set; }
            public List<ClientInfo> Clients { get; set; }
            public List<AnnouncementMessage> Languages { get; set; }
        }

        public class ClientInfo
        {
            public Guid ClientId { get; set; }
            public string ClientCode { get; set; }
        }
        #endregion
    }
}
