using Backoffice.Shared.Models;

namespace Backoffice.Web.Models
{
    public class PrematchSettingsTemplateModel
    {
        public string FirstKey { get; set; }
        public string LastKey { get; set; }
        public List<KeyTimeModel> Settings { get; set; }

        public int SportId { get; set; }
        public string MarketTemplateId { get; set; }
    }
}
