namespace Backoffice.Web.Models.OddsEngine.Config
{
    public class ConfigMyTemplateModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public int SportId { get; set; }
        public string SportName { get; set; }
        public string ClientAssigned {  get; set; }
        public string LeagueAssigned { get; set; }
        public string DateCreated { get; set; }
        public bool IsReadOnly { get; set; }
    }
}
