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

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }

        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

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
                Client = new BoostedSettingsModel();
                Client = ModalParameters?.TryGet<BoostedSettingsModel>("Client");
                TradingService.Region = ModalParameters.TryGet<string>("Region").ToUpper();
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
            ModalService.Cancel();
        }
        public async Task OnSaveClickEvent()
        {
            IsPreLoader = true;

            if (Cap != CapEdit)
            {
                var result = await TradingService.AddOrUpdateBoostedSetting(new BoostedSettingsModel()
                {
                    ClientId = this.ClientId,
                    ClientCode = this.ClientCode,
                    ClientName = this.ClientName,
                    Cap = this.CapEdit != 0 ? this.CapEdit / 100 : 0
                }, UserModel.Id);

                if (result.StatusCode == StatusCode.Success)
                {
                    ShowAlertMessage = false;
                    AlertMessage = "% Cap has been successfully updated!";
                    AlertStatus = "Success!";
                    Opacity = 1.0m;
                }
                else
                {
                    ShowAlertMessage = true;
                    AlertMessage = result.Message.ToString();
                    AlertStatus = "Error!";
                    Opacity = 1.0m;
                }
            }

            await Modal.OnSaveClickCallback.InvokeAsync(true);
            ModalService.Cancel();

            IsPreLoader = false;
        }
        public void Dispose()
        {

        }

        #endregion
    }
}
