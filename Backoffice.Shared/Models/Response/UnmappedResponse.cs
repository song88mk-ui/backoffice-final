using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class UnmappedResponse
    {
        public UnmappedResponse()
        {
            Unmapped = new List<UnmappedModel>();
        }
        public List<UnmappedModel> Unmapped { get; set; }
    }

    public class UnmappedModel
    {
        public string Id { get; set; }
        public string Na { get; set; }
        public int ChTyId { get; set; }
    }
}
