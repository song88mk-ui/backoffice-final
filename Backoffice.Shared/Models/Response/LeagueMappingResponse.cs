using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class LeagueMappingResponse
    {
        public LeagueMappingResponse()
        {
            LeagueMapping = new List<LeagueMappingModel>();
        }
        public List<LeagueMappingModel> LeagueMapping { get; set; }
    }

    public class LeagueMappingModel
    {
        public int LeId { get; set; }
        public string LeNa { get; set; }
        public DateTime Ti { get; set; }
        public ProviderModel<List<int>> Info { get; set; }
        public int ChTyId { get; set; }
    }
}
