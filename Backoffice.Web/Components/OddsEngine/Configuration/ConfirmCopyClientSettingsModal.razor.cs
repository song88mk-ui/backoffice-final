using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ConfirmCopyClientSettingsModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IMessageBroker MessageBroker { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }        
        private KeyValuePair<string, string> SelectedFromClient { get; set; }
        private KeyValuePair<string, string> SelectedToClient { get; set; }
        private const string ToastTitle = "Copy Client Settings";
        private const string ToastMessage = "Success! copy client settings is being generated system will notify once done";
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            SelectedFromClient = ModalParameters.TryGet<KeyValuePair<string, string>>("SelectedFromClient");
            SelectedToClient = ModalParameters.TryGet<KeyValuePair<string, string>>("SelectedToClient");
        }
        public async Task OnCancelModalClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center");
            parameters.Add("ModalDialogClass", "modal-md");

            var options = new ModalOptions();
            ModalService.Open<CopyClientSettingsModal>($"Copy Client Settings", parameters);
        }

        public async Task OnApplyModalClickEvent()
        {
            var payload = new OnCopyClientSettingsMessage
            {
                UserId = UserModel.Id,
                FromClientCode = SelectedFromClient.Key,
                ToClientCode = SelectedToClient.Key
            };

            MessageBroker.Publish(JsonSerializer.Serialize(payload), "OnCopyClientSettings", "r.oe.web.config.oncopyclientsettings", Guid.NewGuid().ToString());

            await ShowToast();

            ModalService.Cancel();
        }
        public void Dispose()
        {
            
        }
        #endregion

        #region Utilities
        public async Task ShowToast()
        {
            await JSRuntime.InvokeVoidAsync("showToast");
            await Task.Delay(500);
        }
        #endregion
    }
}
