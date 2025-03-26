using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class ProviderMappingRequest
    {
        public string UserId { get; set; }
        public ProviderMappingModel Mapping { get; set; }
    }
    public class ProviderMappingModel
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
        public string UserId { get; set; }
        public ProviderMappingBulkRequest()
        {
            Mapping = new List<ProviderMappingModel>();
        }
        public List<ProviderMappingModel> Mapping { get; set; }
    }
}
