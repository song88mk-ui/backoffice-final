using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class DeleteTemplateModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackofficeService BackofficeService { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        //public ConfigTemplateModel ConfigTemplateModel { get; set; }

        #endregion
        private bool _isPreloader;
        private bool _isReadOnly;
        private string _templateName;
        private long _templateId;

        #region Methods

        protected override void OnInitialized()
        {
            BackofficeService.Page = SportsbookClientPermission.SBC_OE_Config.ToString().Replace("_", ".");
            _templateId = ModalParameters.TryGet<long>("ConfigTemplateId");
            _templateName = ModalParameters.TryGet<string>("ConfigTemplateName");
            _isReadOnly = ModalParameters.TryGet<bool>("IsReadOnly");
        }

        public void OnCancelClickEvent() => ModalService.Cancel();

        public async Task OnApplyClickEvent()
        {
            _isPreloader = true;
            var response = await BackofficeService.DeleteConfigTemplate(_templateId);

            if (response.StatusCode == StatusCode.Success)
            {
                await Modal.OnSaveClickCallback.InvokeAsync($"{_templateName} has successfully been removed.");
            }

            _isPreloader = false;
            ModalService.Cancel();
        }

        public void OnCloseClickEvent()
        {
            ModalService.Cancel();
        }

        public void Dispose()
        {

        }
        #endregion
    }
}
