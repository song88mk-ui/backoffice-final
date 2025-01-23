using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Models
{
    public class PriceBoostLogFilterModel
    {
        public List<KeyValuePair<string, string>> Clients { get; set; }
        public SportsbookClientPermission Tab { get; set; }
        public string ClientCode { get; set; }
        //public SportsLookupResponse Sports { get; set; }
        public int SportId { get; set; }
        //public SegmentLookupResponse Segments { get; set; }
        public int SegmentId { get; set; }
        //public LeagueLookupResponse Leagues { get; set; }
        public int LeagueId { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string SearchText { get; set; }
    }
}
