using Backoffice.Shared.Enums;
using Backoffice.Web.Models.OddsEngine.Config;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ConfigNavMenu : IDisposable
    {
        #region Properties

        [Parameter] public ConfigNavMenuParamModel ParamModel { get; set; }
        [Parameter] public EventCallback<ConfigTemplateNavMenu> OnConfigNavMenuClick { get; set; }
        [Parameter] public EventCallback OnCopyClientSelect { get; set; }

        #endregion

        #region Fields

        private ConfigTemplateNavMenu _currentMenu;

        private string _myTemplateText;
        private string _assignedText;
        private string _unAssignedText;

        private string _myTemplateCss;
        private string _assignedCss;
        private string _unAssignedCss;

        private string _calculatingText;
        private int _myTemplateCount;
        private int _assignedCount;
        private int _unAssignedCount;

        private LanguageCode _languageCode;
        private bool _init = true;
        private bool _shouldRender = true;

        private Action<MouseEventArgs> _myTemplateClickEvent;
        private Action<MouseEventArgs> _assignedClickEvent;
        private Action<MouseEventArgs> _unAssignedClickEvent;

        #endregion

        #region Methods

        protected override void OnInitialized()
        {
            _myTemplateClickEvent = async _ => await HandleConfigNavMenuClick(ConfigTemplateNavMenu.MyTemplates);
            _assignedClickEvent = async _ => await HandleConfigNavMenuClick(ConfigTemplateNavMenu.Assigned);
            _unAssignedClickEvent = async _ => await HandleConfigNavMenuClick(ConfigTemplateNavMenu.UnAssigned);
        }

        protected override void OnParametersSet()
        {
            _shouldRender = ValidateParameters();

            if (_shouldRender)
            {
                _languageCode = _translationState.ActiveLanguageCode;
                _calculatingText = $"{_translationState.GetValue("Calculating")}...";

                _currentMenu = ParamModel.SelectedNavMenu;
                _myTemplateCount = ParamModel.MyTemplatesCount;
                _assignedCount = ParamModel.AssignedLeaguesCount;
                _unAssignedCount = ParamModel.UnAssignedLeaguesCount;

                _myTemplateText = $"{_translationState.GetValue("My Templates")} ({FormatCount(_myTemplateCount)})";
                _unAssignedText = $"{_translationState.GetValue("UnAssigned")} ({FormatCount(_unAssignedCount)})";
                _assignedText = $"{_translationState.GetValue("Assigned")} ({FormatCount(_assignedCount)})";

                _myTemplateCss = _currentMenu == ConfigTemplateNavMenu.MyTemplates ? "config-nav active" : "config-nav";
                _unAssignedCss = _currentMenu == ConfigTemplateNavMenu.UnAssigned ? "config-nav active" : "config-nav";
                _assignedCss = _currentMenu == ConfigTemplateNavMenu.Assigned ? "config-nav active" : "config-nav";
            }

            _init = false;
        }

        private string FormatCount(int count)
        {
            if (count == ConfigNavMenuParamModel.CountLoading)
                return _calculatingText;

            return count < 0 ? _translationState.GetValue("Error") : count.ToString();
        }

        protected override bool ShouldRender() => _shouldRender;

        protected override void OnAfterRender(bool firstRender) => _shouldRender = false;

        public async Task HandleConfigNavMenuClick(ConfigTemplateNavMenu configNavMenu)
        {
            await OnConfigNavMenuClick.InvokeAsync(configNavMenu);

            _shouldRender = true;
        }

        public async Task HandleCopyClientSettingsSelect() => await OnCopyClientSelect.InvokeAsync();

        public bool ValidateParameters() => _init || _languageCode != _translationState.ActiveLanguageCode || ParamModel.MyTemplatesCount != _myTemplateCount || ParamModel.AssignedLeaguesCount != _assignedCount || ParamModel.UnAssignedLeaguesCount != _unAssignedCount || _currentMenu != ParamModel.SelectedNavMenu;

        public void Dispose()
        {

        }

        #endregion

    }
}
