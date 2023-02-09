using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Clients
{
    public partial class ViewBetSettingsModal : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        protected List<ClientListResponse.BetSetting> BetSettings { get; set; }
        protected List<string> Currencies { get; set; }
        protected string SelectedCurrency { get; set; }
        protected ClientListResponse.BetSetting SelectedSetting { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            BetSettings = ModalParameters.TryGet<List<ClientListResponse.BetSetting>>("BetSettings");

            if (BetSettings != null)
            {
                Currencies = BetSettings.Select(s => s.CurrencyCode).ToList();
                SelectedCurrency = Currencies.FirstOrDefault() ?? string.Empty;
                SelectedSetting = BetSettings.FirstOrDefault(b => b.CurrencyCode == SelectedCurrency) ?? new ClientListResponse.BetSetting();
            }
            else
            {
                Currencies = new List<string>();
                SelectedCurrency = string.Empty;
                SelectedSetting = new ClientListResponse.BetSetting();
            }

        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnCurrencyClicked(ChangeEventArgs e)
        {
            SelectedCurrency = e.Value.ToString();
            SelectedSetting = BetSettings.FirstOrDefault(b => b.CurrencyCode == SelectedCurrency) ?? new ClientListResponse.BetSetting();
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
