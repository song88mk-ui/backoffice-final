using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Models.OddsEngine.Mapping
{
    public class MappingProviderItemModel
    {
        public MappingProviderItemModel() { }
        public MappingProviderItemModel(UnmappedModel model)
        {
            SpId = model.SpId;
            SpNa = model.SpNa;
            SegId = model.SegId;
            SegNa = model.SegNa;
            LeId = model.LeId;
            LeNa = model.LeNa;
            FiId = model.FiId;
            FiNa = model.FiNa;
            ChTyId = model.ChTyId;
            OtPrLi = model.OtPrLi;
            HasLinkedItem = model.OtPrLi.SrId.Count() > 0 || model.OtPrLi.BgId.Count() > 0;
        }
        public string SpId { get; set; }
        public string SpNa { get; set; }
        public string SegId { get; set; }
        public string SegNa { get; set; }
        public string LeId { get; set; }
        public string LeNa { get; set; }
        public string FiId { get; set; }
        public string FiNa { get; set; }
        public int ChTyId { get; set; }
        public ProviderModel<List<int>> OtPrLi { get; set; }
        public bool IsCurrentLinkedItem { get; set; } = false;
        public bool HasLinkedItem { get; set; }
    }
}
