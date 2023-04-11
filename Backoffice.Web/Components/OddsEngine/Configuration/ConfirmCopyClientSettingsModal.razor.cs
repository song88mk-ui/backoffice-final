using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Drawing;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ConfirmCopyClientSettingsModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IMessageBroker MessageBroker { get; set; }
        
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }        

        private EventCallback OnCopyClientSettingsCallback { get; set; }
        private string Region { get; set; }
        private KeyValuePair<string, string> SelectedFromClient { get; set; }
        private KeyValuePair<string, string> SelectedToClient { get; set; }        
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            SelectedFromClient = ModalParameters.TryGet<KeyValuePair<string, string>>("SelectedFromClient");
            SelectedToClient = ModalParameters.TryGet<KeyValuePair<string, string>>("SelectedToClient");
            Region = ModalParameters.TryGet<string>("Region");
            OnCopyClientSettingsCallback = ModalParameters.TryGet<EventCallback>("OnCopyClientSettingsCallback");
        }
        public async Task OnCancelModalClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center");
            parameters.Add("ModalDialogClass", "modal-md");
            parameters.Add("Region", Region);

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

            MessageBroker.Publish(JsonSerializer.Serialize(payload), "OnCopyClientSettings", "r.oe.web.config.oncopyclientsettings", Guid.NewGuid().ToString(), Region);

            ModalService.Cancel();

            await OnCopyClientSettingsCallback.InvokeAsync();
        }
        public void Dispose()
        {
            
        }
        #endregion        
    }
}
