using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.ParlayBonus
{
    public partial class Filter : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Parameter] public ClientParlayBonusModel ClientParlayBonusResponse { get; set; }        
        [Parameter] public EventCallback<bool> OnUpdateClickCallback { get; set; }
        [Parameter] public EventCallback<ParlayBonusConfig> OnAddClickCallback { get; set; }
        [Parameter] public EventCallback<long> OnClientChangedCallback { get; set; }
        [Parameter] public EventCallback OnSaveClickCallback { get; set; }
        [Parameter] public EventCallback OnCancelClickCallback { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [Parameter] public List<Models.SelectListItemModel<ClientModel.Client>> Clients { get; set; }        
        public string ClientId { get; set; }
        public bool IsUpdating { get; set; } = false;        
        public decimal OriginalMinOdds { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {                   
        }

        public void Dispose()
        {
            
        }

        public async Task OnClientChangedEvent(ChangeEventArgs e)
        {
            IsUpdating = false;
            await OnClientChangedCallback.InvokeAsync(long.Parse(e.Value.ToString()));
        }

        public async Task OnUpdateClickEvent()
        {
            OriginalMinOdds = ClientParlayBonusResponse.ParlayBonusMinOdds;
            IsUpdating = !IsUpdating;

            await OnUpdateClickCallback.InvokeAsync(IsUpdating);

            await InvokeAsync(StateHasChanged);
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
            await InvokeAsync(StateHasChanged);
            await OnCancelClickCallback.InvokeAsync();            
        }

        public async Task OnAddClickEvent()
        {            
            await OnAddClickCallback.InvokeAsync(new ParlayBonusConfig() {
                SelectionCount = ClientParlayBonusResponse.ParlayBonusConfig.Last().SelectionCount + 1
            });
        }
        #endregion
    }
}
