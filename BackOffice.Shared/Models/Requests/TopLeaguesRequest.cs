using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class TopLeaguesRequest
    {
        public string ClientIdRefs { get; set; }
        public int SportId { get; set; }
        public string LanguageCode { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }

        #region Nested Class
        public class TopLeague
        {
            public int SegmentId { get; set; }

            public int LeagueId { get; set; }


            public string SegmentName { get; set; }

            public string LeagueName { get; set; }
        }

        public class ClientTopLeague : TopLeague
        {
            public int SportId { get; set; }
            public string SportName { get; set; }
        }

        public class OrderedClientTopLeague : ClientTopLeague
        {
            public bool Status { get; set; }
            public int SortOrder { get; set; }
        }
        #endregion
    }
}
