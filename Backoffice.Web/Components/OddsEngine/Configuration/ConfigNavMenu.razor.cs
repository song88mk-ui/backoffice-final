using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ConfigNavMenu : IDisposable
    {
        #region Properties

        [Parameter] public string Region { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        [Parameter] public EventCallback<ConfigTemplateNavMenu> SelectConfigNavMenuCallback { get; set; }

        [Inject] private IBackOfficeService BackOfficeService { get; set; }

        public TemplateConfigCount TemplateConfigCount { get; set; }

        public int MyTemplatesCount { get; set; }
        public int AssignedLeaguesCount { get; set; }
        public int UnAssignedLeaguesCount { get; set; }
        public ConfigTemplateNavMenu CurrentMenu { get; set; }

        #endregion


        #region Methods

        protected override async Task OnInitializedAsync()
        {
            CurrentMenu = (int)ConfigTemplateNavMenu.MyTemplates;

            TemplateConfigCount = new TemplateConfigCount();
            MyTemplatesCount = -1;
            AssignedLeaguesCount = -1;
            UnAssignedLeaguesCount = -1;

            await InvokeAsync(async () =>
            {
                var response = await BackOfficeService.GetTemplateConfigCount();
                MyTemplatesCount = response.Result;
                StateHasChanged();
            });
            await InvokeAsync(async () =>
            {
                var response = await BackOfficeService.GetTemplateConfigCount(ConfigState.Assigned.ToString());
                AssignedLeaguesCount = response.Result;
                StateHasChanged();
            });
            await InvokeAsync(async () =>
            {
                var response = await BackOfficeService.GetTemplateConfigCount(ConfigState.UnAssigned.ToString());
                UnAssignedLeaguesCount = response.Result;
                StateHasChanged();
            });
            await SelectConfigNavMenuCallback.InvokeAsync(CurrentMenu);
        }

        public async Task OnSelectConfigNavMenu(ConfigTemplateNavMenu configNavMenu)
        {
            CurrentMenu = configNavMenu;
            await SelectConfigNavMenuCallback.InvokeAsync(configNavMenu);
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
