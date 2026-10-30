using Backoffice.Shared.Enums;

namespace Backoffice.Web.Models.OddsEngine.Config
{
    public class ConfigNavMenuParamModel
    {
        // A count is a number of 0 or more, or one of these two states.
        public const int CountLoading = -1;
        public const int CountFailed = -2;

        public int MyTemplatesCount { get; set; } = CountLoading;
        public int AssignedLeaguesCount { get; set; } = CountLoading;
        public int UnAssignedLeaguesCount { get; set; } = CountLoading;
        public ConfigTemplateNavMenu SelectedNavMenu { get; set; }
    }
}
