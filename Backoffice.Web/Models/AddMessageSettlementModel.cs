using System.ComponentModel.DataAnnotations;

namespace Backoffice.Web.Models
{
    public class AddMessageSettlementModel
    {
        [Required]
        public string BetId { get; set; }
        public DateTime BetDate { get; set; }
        public List<BetSelection> BetSelections { get; set; }
    }

    public class BetSelection
    {
        public bool IsChecked { get; set; }
        public string BetSelectionId { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public long FixtureId { get; set; }
        public string FixtureName { get; set; }
        public string MarketTemplateId { get; set; }
        public int InitialBetSelectionStatusId { get; set; }
        public int ProductId { get; set; }
        public string SelectionSpecifier { get; set; }
        public string FixtureSpecifier { get; set; }
        public int RequestId { get; set; }
        public string Reason { get; set; }
        public Dictionary<string, object> Required { get; set; }
    }
}
