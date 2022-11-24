using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ClientAllLeaguesRequest
    {
        public int ClientIdRef { get; set; }
        public int SportId { get; set; }
        public string LanguageCode { get; set; }
    }
}
