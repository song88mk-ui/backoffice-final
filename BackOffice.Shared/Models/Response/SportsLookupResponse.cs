using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class SportsLookupResponse
    {
        public SportsLookupResponse()
        {
            Sports = new List<SportLookup>();
        }
        public List<SportLookup> Sports { get; set; }        
    }

    public class SportLookup
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
    }
}
