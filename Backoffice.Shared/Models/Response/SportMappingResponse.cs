using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class SportMappingResponse
    {
        public SportMappingResponse()
        {
            SportMapping = new List<SportMappingModel>();
        }
        public List<SportMappingModel> SportMapping { get; set; }
    }

    public class SportMappingModel
    {
        public int SpId { get; set; }
        public string SpNa { get; set; }
        public ProviderModel<int> Info { get; set; }
        public int ChTyId { get; set; }
    }
}
