namespace Backoffice.Web.Models
{
    public class ConfigStateFilterModel
    {
        public List<string> ClientCodes { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int ScopeId { get; set; }
        public string ScopeName { get; set; }
        public string SearchText { get; set; }
    }
}
