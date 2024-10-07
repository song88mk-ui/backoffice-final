using Backoffice.Shared.Enums;

namespace Backoffice.Web.Models.OddsEngine.Config
{
    public class ConfigNavMenuParamModel
    {
        public int MyTemplatesCount { get; set; }
        public int AssignedLeaguesCount { get; set; }
        public int UnAssignedLeaguesCount { get; set; }
        public ConfigTemplateNavMenu SelectedNavMenu { get; set; }
    }
}
