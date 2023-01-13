namespace Backoffice.Web.Models
{
    public class SportsLookupModel
    {
        public SportsLookupModel()
        {
            SportLookup = new List<SportLookup>();
        }
        public List<SportLookup> SportLookup { get; set; }
    }
    public class SportLookup
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
    }
}
