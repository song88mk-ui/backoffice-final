namespace Backoffice.Web.Models.OddsEngine.SportSetting
{
    public class SportSettingsActivityLog
    {
        public DateTime Timestamp { get; set; }
        public string  ClientName { get; set; }
        public string ChangesFrom { get; set; }
        public string ChangesTo { get; set; }
        public string UpdatedBy { get; set; }
    }
}
