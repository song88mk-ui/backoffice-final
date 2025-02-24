using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class SelectionMappingResponse
    {
        public SelectionMappingResponse()
        {
            SelectionMapping = new List<SelectionMappingModel>();
        }
        public List<SelectionMappingModel> SelectionMapping { get; set; }
    }

    public class SelectionMappingModel
    {
        public string SeTeId { get; set; }
        public string SeTeNa { get; set; }
        public ProviderModel<List<string>> Info { get; set; }
    }
}
