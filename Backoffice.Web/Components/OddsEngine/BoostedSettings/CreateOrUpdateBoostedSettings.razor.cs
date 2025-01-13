using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Serilog;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.BoostedSettings
{
    public partial class CreateOrUpdateBoostedSettings : IDisposable
    {
        #region Properties

        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        public ElementReference SaveButton { get; set; }

        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public decimal Cap { get; set; }
        public decimal CapEdit { get; set; }
        public BoostedSettingsModel Client { get; set; }

        protected bool IsPreLoader { get; set; } = false;
        public bool ShowAlertMessage { get; set; }
        public string AlertMessage { get; set; }
        public string AlertStatus { get; set; }
        public decimal Opacity { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Client = ModalParameters?.TryGet<BoostedSettingsModel>("Client") ?? new BoostedSettingsModel();
                ClientId = Client.ClientId;
                ClientCode = Client.ClientCode;
                ClientName = Client.ClientName;
                Cap = Client.Cap * 100;
                CapEdit = decimal.Parse(Cap.ToString("G4"));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }
        }
        public async Task OnCancelClickEvent()
        {
            _modalService.Cancel();
        }
        public async Task OnSaveClickEvent()
        {
            var isClosing = true;
            ShowAlertMessage = false;
            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);

            if (Cap != CapEdit)
            {
                var payload = new BoostedSettingsModel()
                {
                    ClientId = ClientId,
                    ClientCode = ClientCode,
                    ClientName = ClientName,
                    Cap = CapEdit != 0 ? CapEdit / 100 : 0
                };

                var result = await _tradingService.AddOrUpdateBoostedSetting(payload, _userState.Id);  

                if (result.StatusCode == StatusCode.Success)
                {
                    var activityLogResponse = await _backofficeService.AddActivityLog(new ActivityLogModel()
                    {
                        Page = SportsbookClientPermission.SBC_OE_PriceBoost_Settings.ToString().Replace('_', '.'),
                        Payload = $"{ClientCode.ToUpper()}",
                        Activity = Activity.Update.ToString(),
                        From = JsonSerializer.Serialize(new BoostedSettingsModel()
                        {
                            ClientId = ClientId,
                            ClientCode = ClientCode,
                            ClientName = ClientName,
                            Cap = Cap / 100
                        }),
                        To = JsonSerializer.Serialize(payload),
                        UserId = _userState.Id,
                        UserName = _userState.Name
                    });
                }
                else
                {
                    isClosing = false;
                    ShowAlertMessage = true;
                    AlertMessage = result.Message.ToString();
                    AlertStatus = "Error!";
                    Opacity = 1.0m;
                }
            }

            IsPreLoader = false;
            if (isClosing)
            {
                await Modal.OnSaveClickCallback.InvokeAsync(true);
                _modalService.Cancel();
            }
        }
        public void Dispose()
        {

        }

        #endregion
    }
}
