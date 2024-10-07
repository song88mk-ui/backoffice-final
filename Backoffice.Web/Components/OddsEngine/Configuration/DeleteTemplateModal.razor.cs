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

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        #endregion
        private bool _isPreloader;
        private bool _isReadOnly;
        private string _templateName;
        private long _templateId;

        #region Methods

        protected override void OnInitialized()
        {
            _backofficeService.Page = SportsbookClientPermission.SBC_OE_Config.ToString().Replace("_", ".");
            _templateId = ModalParameters.TryGet<long>("ConfigTemplateId");
            _templateName = ModalParameters.TryGet<string>("ConfigTemplateName");
            _isReadOnly = ModalParameters.TryGet<bool>("IsReadOnly");
        }

        public void OnCancelClickEvent() => _modalService.Cancel();

        public async Task OnApplyClickEvent()
        {
            _isPreloader = true;
            var response = await _backofficeService.DeleteConfigTemplate(_templateId);

            if (response.StatusCode == StatusCode.Success)
            {
                await Modal.OnSaveClickCallback.InvokeAsync($"{_templateName} has successfully been removed.");
            }

            _isPreloader = false;
            _modalService.Cancel();
        }

        public void OnCloseClickEvent() => _modalService.Cancel();

        public void Dispose()
        {

        }
        #endregion
    }
}
