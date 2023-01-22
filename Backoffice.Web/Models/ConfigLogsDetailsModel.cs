namespace Backoffice.Web.Models
{
    public class ConfigLogsDetailsModel
    {
        public int Id { get; set; }
        public string Date { get; set; }
        public string Client { get; set; }
        public string Sports { get; set; }
        public string Segment { get; set; }
        public string League { get; set; }
        public string ChangesMade { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string UpdatedBy { get; set; }
    }
}
