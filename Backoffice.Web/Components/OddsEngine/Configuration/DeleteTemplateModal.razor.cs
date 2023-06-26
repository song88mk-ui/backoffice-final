using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class DeleteTemplateModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackOfficeService BackofficeService { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        public ConfigTemplateModel ConfigTemplateModel { get; set; }

        #endregion


        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ConfigTemplateModel = ModalParameters.TryGet<ConfigTemplateModel>("ConfigTemplateModel");
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnApplyClickEvent()
        {
            var response = await BackofficeService.DeleteConfigTemplate(UserModel.Id, ConfigTemplateModel.Id);

            if (response.StatusCode == StatusCode.Success)
            {
                await Modal.OnSaveClickCallback.InvokeAsync(true);
            }

            ModalService.Cancel();
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
