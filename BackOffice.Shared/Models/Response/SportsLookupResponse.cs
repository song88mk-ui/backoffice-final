using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Backoffice.Shared.Models.Response
{
    public class SportsLookupResponse
    {
        public SportsLookupResponse()
        {
            SportLookup = new List<SportLookup>();
        }
        public List<SportLookup> SportLookup { get; set; }        
    }

    public class SportLookup
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
    }
}
