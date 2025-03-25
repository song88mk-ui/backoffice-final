using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class SegmentMappingResponse
    {
        public SegmentMappingResponse()
        {
            SegmentMapping = new List<SegmentMappingModel>();
        }
        public List<SegmentMappingModel> SegmentMapping { get; set; }
        public int TotalCount { get; set; }
        public int TotalPage { get; set; }
    }

    public class SegmentMappingModel
    {
        public int SegId { get; set; }
        public string SegNa { get; set; }
        public DateTime Ti { get; set; }
        public ProviderModel<List<int>> Info { get; set; }
        public int? ChTyId { get; set; }
    }
}
