using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

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
        public BoostedSettingsModel BoostedSettings { get; set; }

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
                BoostedSettings = new BoostedSettingsModel();
                BoostedSettings = ModalParameters?.TryGet<BoostedSettingsModel>("Client");
                //ClientCode = ModalParameters.TryGet<string>("ClientCode");
                //ClientName = ModalParameters.TryGet<string>("ClientName");
                //Cap = ModalParameters.TryGet<decimal>("Cap") * 100;
                //CapEdit = decimal.Parse(Cap.ToString("G4"));
            }
            catch (Exception)
            {

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

                    await Modal.OnSaveClickCallback.InvokeAsync(true);
                    await Task.Delay(3500);
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
            IsPreLoader = true;
        }
        public void Dispose()
        {

        }
        #endregion
    }
}
