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
        [Parameter] public int CurrentMenu { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        [Inject] private IBackOfficeService BackOfficeService { get; set; }

        public TemplateConfigCount TemplateConfigCount { get; set; }

        #endregion


        #region Methods

        protected override async Task OnInitializedAsync()
        {
            TemplateConfigCount = new TemplateConfigCount();

            TemplateConfigCount = (await BackOfficeService.GetTemplateConfigCount(UserModel.Id)).Result;
        }

        public void Dispose()
        {
            
        }

        #endregion
    }
}
