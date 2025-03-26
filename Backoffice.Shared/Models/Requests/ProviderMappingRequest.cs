using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class ProviderMappingRequest
    {
        public string Id { get; set; }
        public string Na { get; set; }
        public string Info { get; set; }
        public int? SpId { get; set; } = null;
        public int? SegId { get; set; } = null;
        public int? LeId { get; set; } = null;
        public int? ChTyId { get; set; }
    }

    public class ProviderMappingBulkRequest
    {
        public Guid UserId { get; set; }
        public ProviderMappingBulkRequest()
        {
            Mapping = new List<ProviderMappingRequest>();
        }
        public List<ProviderMappingRequest> Mapping { get; set; }
    }
}
