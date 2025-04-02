namespace Backoffice.Web.Models.OddsEngine.Mapping
{
    public class Info
    {
        public List<int> SrId { get; set; }
        public List<int> BgId { get; set; }
    }

    public class LeagueMappingModel
    {
        public int LeId { get; set; }
        public string LeNa { get; set; }
        public string Ti { get; set; }
        public Info Info { get; set; }

    }
}