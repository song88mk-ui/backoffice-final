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
        public UnmappedModel()
        {
            OtPrLi = new ProviderModel<List<int>>();
            OtPrLi.SrId = new List<int>();
            OtPrLi.BgId = new List<int>();
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
    }
}
