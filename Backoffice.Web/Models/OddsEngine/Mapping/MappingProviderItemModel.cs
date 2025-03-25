using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Models.OddsEngine.Mapping
{
    public class MappingProviderItemModel
    {
        public MappingProviderItemModel() { }
        public MappingProviderItemModel(UnmappedModel model)
        {
            Id = model.Id;
            Na = model.Na;
            ChTyId = model.ChTyId;
            OtPrLi = model.OtPrLi;
            HasLinkedItem = model.OtPrLi.SrId.Count() > 0 || model.OtPrLi.BgId.Count() > 0;
        }
        public string Id { get; set; }
        public string Na { get; set; }
        public int ChTyId { get; set; }
        public ProviderModel<List<int>> OtPrLi { get; set; }
        public bool IsCurrentLinkedItem { get; set; } = false;
        public bool HasLinkedItem { get; set; }
    }
}
