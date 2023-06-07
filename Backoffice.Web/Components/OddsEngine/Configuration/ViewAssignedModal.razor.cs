using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ViewAssignedModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        private ConfigTemplateModel ConfigTemplateModel { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ConfigTemplateModel = ModalParameters.TryGet<ConfigTemplateModel>("ConfigTemplateModel");
        }

        public async Task OnCloseClickEvent()
        {
            ModalService.Cancel();
        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
