using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.ComponentModel;

namespace Backoffice.Web.Components.SBC.ParlayBonus
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Parameter] public ClientParlayBonusModel ClientParlayBonusResponse { get; set; }
        [Parameter] public EventCallback<bool> OnUpdateClickCallback { get; set; }
        [Parameter] public EventCallback OnAddClickCallback { get; set; }
        [Parameter] public EventCallback<long> OnClientChangedCallback { get; set; }
        [Parameter] public EventCallback OnSaveClickCallback { get; set; }
        [Parameter] public EventCallback OnCancelClickCallback { get; set; }
        [Parameter] public EventCallback<bool> IsEditChanged { get; set; }
        [Parameter] public bool IsEdit { get; set; }
        [Parameter] public bool IsDisableAction { get; set; }

        public ElementReference ParlayBonusMinOddsElementReference { get; set; }
        public ElementReference FilterAddButtonElementReference { get; set; }
        private List<SelectListItemModel<Client>> _clients { get; set; }

        public string ClientId { get; set; }
        public decimal OriginalMinOdds { get; set; }
        public static bool IsFormValid { get; set; }
        private int InvalidCount { get; set; }
        public bool IsSaveDisabled { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ClientId = "0";

            if(_userState.Model.SportsbookClients != null && _userState.Model.SportsbookClients.Any())
            {
                await LoadClients();
            }

            _userState.PropertyChanged += UserStatePropertyChanged;
        } 

        public async Task OnClientChangedEvent(ChangeEventArgs e)
        {
            IsEdit = false;
            ClientId = e.Value.ToString();
            await OnClientChangedCallback.InvokeAsync(long.Parse(e.Value.ToString()));
        }

        public async Task OnUpdateClickEvent()
        {
            OriginalMinOdds = ClientParlayBonusResponse.ParlayBonusMinOdds;
            IsEdit = !IsEdit;
            IsSaveDisabled = false;

            IsFormValid = ClientParlayBonusResponse.ParlayBonusMinOdds < 1.20m ? true : false;

            await OnUpdateClickCallback.InvokeAsync(IsEdit);
        }

        public async Task OnSaveClickEvent()
        {
            IsEdit = false;
            await OnSaveClickCallback.InvokeAsync();
        }

        public async Task OnCancelClickEvent()
        {
            ClientParlayBonusResponse.ParlayBonusMinOdds = OriginalMinOdds;
            IsEdit = !IsEdit;

            await OnCancelClickCallback.InvokeAsync();            
        }

        public async Task OnAddClickEvent()
        {
            IsSaveDisabled = true;
            await OnAddClickCallback.InvokeAsync();
        }

        public async Task FilterAddBtnOnMouseover()
        {
            FilterAddButtonElementReference.FocusAsync();
        }

        public async Task ValidateParlayBonusMinOdds(ChangeEventArgs e)
        {
            await _jsRuntime.InvokeVoidAsync("ValidateParlayBonusMinOdds", ParlayBonusMinOddsElementReference);
        }

        [JSInvokable("DisableSaveButton")]
        public static async Task DisableSaveButton(bool isDisabled)
        {
            IsFormValid = isDisabled;
        }

        public void Dispose()
        {

        }

        #endregion

        #region Utilities
        private async void UserStatePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            await LoadClients();
        }
        private async Task LoadClients()
        {
            if (_userState.Model.SportsbookClients != null && _userState.Model.SportsbookClients.Any())
            {
                _clients = _userState.Model.SportsbookClients.OrderBy(x => x.ClientName).Select(s => new SelectListItemModel<Client>
                {
                    Value = s,
                    Name = s.ClientName
                }).ToList();
            }

            if (!_clients.Any())
                _clients.Add(new SelectListItemModel<Client>
                {
                    Value = new Client(),
                    Name = _translationState.GetValue("No Clients Available")
                });
            else
                _clients.Insert(0, new SelectListItemModel<Client> { Value = new Client(), Name = "Choose client" });

            var clientId = _clients.FirstOrDefault()?.Value.ClientId ?? 0;

            await OnClientChangedCallback.InvokeAsync(clientId);
        }
        #endregion
    }
}
