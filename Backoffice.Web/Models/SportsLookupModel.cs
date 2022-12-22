namespace Backoffice.Web.Models
{
    public class SportsLookupModel
    {
        public SportsLookupModel()
        {
            Sports = new List<SportLookup>();
        }
        public List<SportLookup> Sports { get; set; }

        #region Nested Class
        public class SportLookup
        {
            public int SportId { get; set; }
            public string SportName { get; set; }
        }
        #endregion
    }
}
