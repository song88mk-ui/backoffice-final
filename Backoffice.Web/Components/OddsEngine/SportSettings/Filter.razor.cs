using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using static Backoffice.Shared.Models.Response.LeagueLookupResponse;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class Filter : IDisposable
    {
        #region Properties
        [Inject] public ITradingService TradingService { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [Parameter] public ClientSportOrderFilterModel ClientSportOrderFilter { get; set; }
        [Parameter] public EventCallback<ClientSportOrderFilterModel> ClientSportOrderFilterChanged { get; set; }
        [Parameter] public EventCallback OnSaveClickCallback { get; set; }
        [Parameter] public EventCallback<string> OnClientChangedCallback { get; set; }
        [Parameter] public EventCallback<string> OnUpdateCallback { get; set; }

        private bool IsUpdate { get; set; }
        private List<KeyValuePair<string, string>> Clients { get; set; } = new List<KeyValuePair<string, string>>();
        private string SelectedClient { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            IsUpdate = false;

            Clients = await GetClients();
            SelectedClient = Clients.FirstOrDefault().Key;

            await OnClientChangedCallback.InvokeAsync(SelectedClient);
        }
        public async void OnClientSelectionChangedEvent(ChangeEventArgs e)
        {
            SelectedClient = e.Value.ToString();

            await InvokeAsync(StateHasChanged);
            await OnClientChangedCallback.InvokeAsync(SelectedClient);
        }
        public async Task OnEditClickEvent()
        {
            IsUpdate = true;
            ClientSportOrderFilter.IsUpdate = true;

            await ClientSportOrderFilterChanged.InvokeAsync(ClientSportOrderFilter);
        }
        public async Task OnSaveClickEvent()
        {
            ClientSportOrderFilter.IsUpdate = false;

            await ClientSportOrderFilterChanged.InvokeAsync(ClientSportOrderFilter);
            await OnSaveClickCallback.InvokeAsync();
        }
        public async Task OnCancelClickEvent()
        {
            IsUpdate = false;
        }
        public void Dispose()
        {
        }

        #endregion
        private async Task<List<KeyValuePair<string, string>>> GetClients()
        {
            var response = await TradingService.GetClientList();

            List<KeyValuePair<string, string>> Clients = new List<KeyValuePair<string, string>>();

            if (response != null && UserModel != null)
            {
                foreach (var client in response.Result)
                {
                    if (UserModel.ClientCodes.Contains(client.ClientCode, StringComparer.OrdinalIgnoreCase))
                    {
                        KeyValuePair<string, string> Client = new KeyValuePair<string, string>(client.ClientCode, client.ClientName);

                        Clients.Add(Client);
                    }
                }
            }

            return Clients;
        }
    }
}
