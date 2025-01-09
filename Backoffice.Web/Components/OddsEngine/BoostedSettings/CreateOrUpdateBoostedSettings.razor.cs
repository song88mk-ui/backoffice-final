using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Serilog;

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
                _tradingService.Region = ModalParameters?.TryGet<string>("Region").ToUpper() ?? "HK";
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
                var result = await _tradingService.AddOrUpdateBoostedSetting(new BoostedSettingsModel()
                {
                    ClientId = this.ClientId,
                    ClientCode = this.ClientCode,
                    ClientName = this.ClientName,
                    Cap = this.CapEdit != 0 ? this.CapEdit / 100 : 0
                }, _userState.Id);

                if (result.StatusCode != StatusCode.Success)
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
