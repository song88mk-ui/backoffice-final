using Backoffice.Shared.Enums;
using Backoffice.Web.Models.OddsEngine.Config;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ConfigNavMenu : IDisposable
    {
        #region Properties

        [Parameter] public ConfigNavMenuParamModel ParamModel { get; set; }
        [Parameter] public EventCallback<ConfigTemplateNavMenu> SelectConfigNavMenuCallback { get; set; }
        [Parameter] public EventCallback SelectCopyClientCallback { get; set; }

        #endregion

        #region Fields

        private ConfigTemplateNavMenu _currentMenu;

        private string _myTemplateTitle;
        private string _assignedTitle;
        private string _unAssignedTitle;

        private int _myTemplateCount;
        private int _assignedCount;
        private int _unAssignedCount;
        private string _calculatingText;

        private LanguageCode _languageCode;
        private bool _init = true;
        private bool _shouldRender = true;

        #endregion

        #region Methods

        protected override void OnParametersSet()
        {
            _shouldRender = ValidateParameters();

            if (_shouldRender)
            {
                _languageCode = TranslationState.ActiveLanguageCode;
                _calculatingText = $"{TranslationState.GetValue("Calculating")}...";

                _myTemplateTitle = TranslationState.GetValue("My Templates");
                _assignedTitle = TranslationState.GetValue("Assigned");
                _unAssignedTitle = TranslationState.GetValue("UnAssigned");

                _currentMenu = ParamModel.SelectedNavMenu;
                _myTemplateCount = ParamModel.MyTemplatesCount;
                _assignedCount = ParamModel.AssignedLeaguesCount;
                _unAssignedCount = ParamModel.UnAssignedLeaguesCount;
            }

            _init = false;
        }

        protected override bool ShouldRender() => _shouldRender;

        protected override void OnAfterRender(bool firstRender) => _shouldRender = false;

        public async Task OnSelectConfigNavMenu(ConfigTemplateNavMenu configNavMenu)
        {
            await SelectConfigNavMenuCallback.InvokeAsync(_currentMenu = configNavMenu);

            _shouldRender = true;
        }

        public async Task SelectCopyClientSettings() => await SelectCopyClientCallback.InvokeAsync();

        public bool ValidateParameters() => _init || _languageCode != TranslationState.ActiveLanguageCode || ParamModel.MyTemplatesCount != _myTemplateCount || ParamModel.AssignedLeaguesCount != _assignedCount || ParamModel.UnAssignedLeaguesCount != _unAssignedCount || _currentMenu != ParamModel.SelectedNavMenu;

        public void Dispose()
        {

        }

        #endregion

    }
}
