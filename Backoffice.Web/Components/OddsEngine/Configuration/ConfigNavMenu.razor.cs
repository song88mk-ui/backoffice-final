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
        public ConfigTemplateNavMenu CurrentMenu { get; set; }

        #endregion


        #region Methods

        protected override async Task OnInitializedAsync()
        {
            CurrentMenu = (int)ConfigTemplateNavMenu.MyTemplates;

            TemplateConfigCount = new TemplateConfigCount();

            TemplateConfigCount = (await BackOfficeService.GetTemplateConfigCount()).Result;

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
