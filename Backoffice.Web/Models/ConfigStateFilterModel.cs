namespace Backoffice.Web.Models
{
    public class ConfigStateFilterModel
    {
        public List<string> ClientIds { get; set; }
        public int SportId { get; set; }
        public int ScopeId { get; set; }
        public string SearchText { get; set; }
    }
}
