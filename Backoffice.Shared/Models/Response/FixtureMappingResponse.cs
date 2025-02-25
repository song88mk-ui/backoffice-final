using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class FixtureMappingResponse
    {
        public FixtureMappingResponse()
        {
            FixtureMapping = new List<FixtureMappingModel>();
        }
        public List<FixtureMappingModel> FixtureMapping { get; set; }
        public int TotalCount { get; set; }
        public int TotalPage { get; set; }
    }

    public class FixtureMappingModel
    {
        public string FiId { get; set; }
        public string FiNa { get; set; }
        public DateTime? FiStDa { get; set; }
        public int SpId { get; set; }
        public int SegId { get; set; }
        public int LeId { get; set; }
        public DateTime Ti { get; set; }
        public ProviderModel<long> Info { get; set; }
        public int ChTyId { get; set; }
    }
}
