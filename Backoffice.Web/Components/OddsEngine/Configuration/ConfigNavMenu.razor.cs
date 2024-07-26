using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Threading;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ConfigNavMenu : IDisposable
    {
        #region Properties

        [Parameter] public string Region { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        [Parameter] public EventCallback<ConfigTemplateNavMenu> SelectConfigNavMenuCallback { get; set; }
        [Parameter] public EventCallback SelectCopyClientCallback { get; set; }
        [Parameter] public int? MyTemplatesCount { get; set; }
        [Parameter] public int? AssignedLeaguesCount { get; set; }
        [Parameter] public int? UnAssignedLeaguesCount { get; set; }

        [Inject] private IBackofficeService BackofficeService { get; set; }

        public TemplateConfigCount TemplateConfigCount { get; set; }

        public ConfigTemplateNavMenu CurrentMenu { get; set; }

        #endregion

        private CancellationTokenSource _cancellationTokenSource;

        #region Methods                                          
        protected override async Task OnInitializedAsync()
        {
            CurrentMenu = (int)ConfigTemplateNavMenu.MyTemplates;

            _cancellationTokenSource = new();

            TemplateConfigCount = new TemplateConfigCount();
            MyTemplatesCount = -1;
            AssignedLeaguesCount = -1;
            UnAssignedLeaguesCount = -1;
            await SelectConfigNavMenuCallback.InvokeAsync(CurrentMenu);
        }

        public async Task OnSelectConfigNavMenu(ConfigTemplateNavMenu configNavMenu)
        {
            CurrentMenu = configNavMenu;
            await SelectConfigNavMenuCallback.InvokeAsync(configNavMenu);
        }

        public async Task SelectCopyClientSettings()
        {
            await SelectCopyClientCallback.InvokeAsync();
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
