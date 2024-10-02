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
        [Inject] private IBackofficeService BackofficeService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }

        [Parameter] public ConfigNavMenuParamModel ParamModel { get; set; }
        [Parameter] public EventCallback<ConfigTemplateNavMenu> SelectConfigNavMenuCallback { get; set; }
        [Parameter] public EventCallback SelectCopyClientCallback { get; set; }

        private ConfigTemplateNavMenu _currentMenu { get; set; }

        private string _myTemplateTitle;
        private string _assignedTitle;
        private string _unAssignedTitle;

        private int _myTemplateCount;
        private int _assignedCount;
        private int _unAssignedCount;
        private string _calculatingText;

        private LanguageCode _languageCode;
        private bool _init;
        private bool _shouldRender;
        #endregion

        #region Methods
        protected override void OnInitialized()
        {
            _currentMenu = (int)ConfigTemplateNavMenu.MyTemplates;
            _init = true;

            Console.WriteLine($"{this.GetType().Name} - Init");
        }

        protected override void OnParametersSet()
        {
            var hasChange = false;

            if (_init || _languageCode != TranslationState.ActiveLanguageCode)
            {
                _languageCode = TranslationState.ActiveLanguageCode;
                _calculatingText = $"{TranslationState.GetValue("Calculating")}...";

                _myTemplateTitle = TranslationState.GetValue("My Templates");
                _assignedTitle = TranslationState.GetValue("Assigned");
                _unAssignedTitle = TranslationState.GetValue("UnAssigned");

                hasChange = true;
            }

            if (_init || ParamModel.MyTemplatesCount != _myTemplateCount || ParamModel.AssignedLeaguesCount != _assignedCount || ParamModel.UnAssignedLeaguesCount != _unAssignedCount)
            {
                _myTemplateCount = ParamModel.MyTemplatesCount;
                _assignedCount = ParamModel.AssignedLeaguesCount;
                _unAssignedCount = ParamModel.UnAssignedLeaguesCount;

                hasChange = true;
            }

            _init = false;
            _shouldRender = hasChange;
            Console.WriteLine($"{this.GetType().Name} - Param {_shouldRender}");
        }
        protected override bool ShouldRender() => _shouldRender;
        private int _renderCount;
        protected override void OnAfterRender(bool firstRender)
        {
            Console.WriteLine($"{this.GetType().Name} - Render {_renderCount}");
            _renderCount++;

            _shouldRender = false;
        }
        public async Task OnSelectConfigNavMenu(ConfigTemplateNavMenu configNavMenu) {
            await SelectConfigNavMenuCallback.InvokeAsync(_currentMenu = configNavMenu);

            _shouldRender = true;
        }

        public async Task SelectCopyClientSettings() => await SelectCopyClientCallback.InvokeAsync();

        public void Dispose()
        {

        }

        #endregion

    }
}
