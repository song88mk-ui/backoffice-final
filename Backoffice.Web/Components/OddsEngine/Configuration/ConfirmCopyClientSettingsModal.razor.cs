using Backoffice.Shared.Data.Trading.SQLEntities;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ConfirmCopyClientSettingsModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackofficeService BackofficeService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        private EventCallback<AlertModel> OnCopyClientSettingsCallback { get; set; }
        private ClientInformationModel ToClient { get; set; }
        private KeyValuePair<string, string> SelectedFromClient { get; set; }
        private bool _isProcessing;
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            SelectedFromClient = ModalParameters.TryGet<KeyValuePair<string, string>>("SelectedFromClient");
            ToClient = ModalParameters.TryGet<ClientInformationModel>("ToClient");
            OnCopyClientSettingsCallback = ModalParameters.TryGet<EventCallback<AlertModel>>("OnCopyClientSettingsCallback");
        }
        public async Task OnCancelModalClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center");
            parameters.Add("ModalDialogClass", "modal-md");
            parameters.Add("OnCopyClientSettingsCallback", OnCopyClientSettingsCallback);
            parameters.Add("FromClientCode", SelectedFromClient.Key);
            parameters.Add("ToClientCode", ToClient.ClientCode);

            ModalService.Open<CopyClientSettingsModal>($"Copy Client Settings", parameters);
        }

        public async Task OnApplyModalClickEvent()
        {
            _isProcessing = true;

            var alert = new AlertModel();
            var payload = new CopyClientConfigRequest
            {
                FromClientCode = SelectedFromClient.Key,
                ToClientCode = ToClient.ClientCode,
                ToClientId = ToClient.ClientId,
                ToClientName = ToClient.ClientName
            };

            var response = await BackofficeService.CopyClientConfig(payload);

            if (response.StatusCode == StatusCode.Success)
            {

                var message = new OnCopyClientSettingsMessage
                {
                    UserId = _userState.Model.Id,
                    FromClientCode = SelectedFromClient.Key,
                    ToClientCode = ToClient.ClientCode,
                    ToRemoveConfig = response.Result.ToBeRemoved.Select(s => new ClientLeagueConfig
                    {
                        ClientCode = s.ClientCode,
                        ClientId = s.ClientId,
                        SportId = s.SportId,
                        SegmentId = s.SegmentId,
                        LeagueId = s.LeagueId,
                    }
                    ).ToList()
                };

                await _tradingService.Publish(new MessageRequest
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    MessageType = "OnCopyClientSettings2",
                    RoutingKey = "r.oe.web.config.oncopyclientsettings2",
                    Payload = JsonSerializer.Serialize(message)
                });

                alert = new AlertModel()
                {
                    Status = AlertStatus.Success,
                    Message = "Copy Client Settings Successfully.",
                    Opacity = 1.0m
                };
            }
            else
                alert = new AlertModel()
                {
                    Status = AlertStatus.Error,
                    Message = "Copy Client Settings Failed.",
                    Opacity = 1.0m
                };

            _isProcessing = false;

            ModalService.Cancel();
            await OnCopyClientSettingsCallback.InvokeAsync(alert);
        }
        public void Dispose()
        {

        }
        #endregion        
    }
}
