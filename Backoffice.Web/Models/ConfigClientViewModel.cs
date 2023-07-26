using Backoffice.Shared.Models;

namespace Backoffice.Web.Models
{
    public class ConfigClientViewModel
    {
        public bool IsChecked { get; set; } = false;
        public long Id { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int FixtureStatusId { get; set; }
        public string FixtureStatusName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public long TemplateId { get; set; } = -1;
        public string TemplateName { get; set; } = "";

        public static implicit operator ConfigClientViewModel(ConfigClientModel payload)
        {
            return new ConfigClientViewModel()
            {
                Id = payload.Id,
                ClientId = payload.ClientId,
                ClientCode = payload.ClientCode,
                ClientName = payload.ClientName,
                SportId = payload.SportId,
                SportName = payload.SportName,
                FixtureStatusId = payload.FixtureStatusId,
                FixtureStatusName = payload.FixtureStatusName,
                SegmentId = payload.SegmentId,
                SegmentName = payload.SegmentName,
                LeagueId = payload.LeagueId,
                LeagueName = payload.LeagueName,
                TemplateId = payload.TemplateId,
                TemplateName = payload.TemplateName
            };
        }
        public static List<ConfigClientViewModel> ConvertList(List<ConfigClientModel> payload)
        {
            return payload.Select(x => new ConfigClientViewModel()
            {
                Id = x.Id,
                ClientId = x.ClientId,
                ClientCode = x.ClientCode,
                ClientName = x.ClientName,
                SportId = x.SportId,
                SportName = x.SportName,
                FixtureStatusId = x.FixtureStatusId,
                FixtureStatusName = x.FixtureStatusName,
                SegmentId = x.SegmentId,
                SegmentName = x.SegmentName,
                LeagueId = x.LeagueId,
                LeagueName = x.LeagueName,
                TemplateId = x.TemplateId,
                TemplateName = x.TemplateName
            }).ToList();
        }
    }
}
