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
                ClientIds = new List<string>();
            }
            public List<string> ClientIds { get; set; }
            public bool IsActive { get; set; }
            public string Title { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Message { get; set; }
        }

        #endregion
    }
}
