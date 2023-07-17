using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Components.SBC.ParlayBonus
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        [Parameter] public ClientParlayBonusModel ClientParlayBonusResponse { get; set; }
        [Parameter] public EventCallback<bool> OnUpdateClickCallback { get; set; }
        [Parameter] public EventCallback OnAddClickCallback { get; set; }
        [Parameter] public EventCallback<long> OnClientChangedCallback { get; set; }
        [Parameter] public EventCallback OnSaveClickCallback { get; set; }
        [Parameter] public EventCallback OnCancelClickCallback { get; set; }
        [Parameter] public EventCallback<bool> IsUpdatingChanged { get; set; }
        [Parameter] public List<SelectListItemModel<Client>> Clients { get; set; }
        [Parameter] public bool IsUpdating { get; set; }

        public ElementReference ParlayBonusMinOddsElementReference { get; set; }
        public ElementReference FilterAddButtonElementReference { get; set; }

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
        } 

        public async Task OnClientChangedEvent(ChangeEventArgs e)
        {
            IsUpdating = false;
            ClientId = e.Value.ToString();
            await OnClientChangedCallback.InvokeAsync(long.Parse(e.Value.ToString()));
        }

        public async Task OnUpdateClickEvent()
        {
            OriginalMinOdds = ClientParlayBonusResponse.ParlayBonusMinOdds;
            IsUpdating = !IsUpdating;

            IsFormValid = ClientParlayBonusResponse.ParlayBonusMinOdds < 1.20m ? true : false;

            await OnUpdateClickCallback.InvokeAsync(IsUpdating);
        }

        public async Task OnSaveClickEvent()
        {
            IsUpdating = false;
            await OnSaveClickCallback.InvokeAsync();
        }

        public async Task OnCancelClickEvent()
        {
            ClientParlayBonusResponse.ParlayBonusMinOdds = OriginalMinOdds;
            IsUpdating = !IsUpdating;

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
            await JSRuntime.InvokeVoidAsync("ValidateParlayBonusMinOdds", ParlayBonusMinOddsElementReference);
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
    }
}
