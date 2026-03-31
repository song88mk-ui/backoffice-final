using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class SportsLookupResponse
    {
        public SportsLookupResponse()
        {
            SportLookup = new List<SportLookupModel>();
        }
        public List<SportLookupModel> SportLookup { get; set; }        
    }

    public class SportLookupModel
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
    }
}
