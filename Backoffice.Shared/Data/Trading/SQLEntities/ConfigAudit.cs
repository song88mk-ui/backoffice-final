using System;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class ConfigAudit
    {
        public int Id { get; set; }
        public int FixtureTypeId { get; set; }
        public DateTime TimeStamp { get; set; }
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public int SportsId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public string MarketTemplateId { get; set; }
        public string MarketTemplateName { get; set; }
        public string OldPrematchSettings { get; set; }
        public string NewPrematchSettings { get; set; }
        public string OldLiveSettings { get; set; }
        public string NewLiveSettings { get; set; }
        public bool? OldStatus { get; set; }
        public bool? NewStatus { get; set; }
        public Guid? UserId { get; set; }
        public string Username { get; set; }
        public bool IsBulkUpload { get; set; }
    }
}
