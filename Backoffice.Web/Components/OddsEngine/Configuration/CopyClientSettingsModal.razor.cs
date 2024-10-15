using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class CopyClientSettingsModal : IDisposable
    {
        #region Properties
        [Inject] private ITradingService TradingService { get; set; }
        [Inject] private IModalService ModalService { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        private EventCallback<AlertModel> OnCopyClientSettingsCallback { get; set; }

        private List<ClientInformationModel> Clients { get; set; }

        private KeyValuePair<string, string> SelectedFromClient { get; set; }
        private KeyValuePair<string, string> SelectedToClient { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Clients = new List<ClientInformationModel>();
            OnCopyClientSettingsCallback = ModalParameters.TryGet<EventCallback<AlertModel>>("OnCopyClientSettingsCallback");

            await Initialize();
        }

        public async Task Initialize()
        {
            Clients = await LoadClients();

            var selectedFrom = ModalParameters.TryGet<string>("FromClientCode");
            var selectedTo = ModalParameters.TryGet<string>("ToClientCode");

            if(!string.IsNullOrEmpty(selectedFrom) && !string.IsNullOrEmpty(selectedTo))
            {
                var clientFrom = Clients.FirstOrDefault(c => c.ClientCode.ToUpper() == selectedFrom.ToUpper());
                var clientTo = Clients.FirstOrDefault(c => c.ClientCode.ToUpper() == selectedTo.ToUpper());

                SelectedFromClient = new KeyValuePair<string, string>(clientFrom?.ClientCode ?? string.Empty, clientFrom?.ClientName ?? string.Empty);
                SelectedToClient = new KeyValuePair<string, string>(clientTo?.ClientCode ?? string.Empty, clientTo?.ClientName ?? string.Empty);
            }
            else
            {
                SelectedFromClient = new KeyValuePair<string, string>(Clients[0].ClientCode, Clients[0].ClientName);
                SelectedToClient = new KeyValuePair<string, string>(Clients[1].ClientCode, Clients[1].ClientName);
            }
        }

        public async Task OnFromClientChangedEvent(ChangeEventArgs e)
        {
            var clientCode = e.Value?.ToString()?.ToUpper() ?? string.Empty;
            var client = Clients.FirstOrDefault(c => c.ClientCode == clientCode);

            SelectedFromClient = new KeyValuePair<string, string>(clientCode, client?.ClientName ?? string.Empty);
        }

        public async Task OnToClientChangedEvent(ChangeEventArgs e)
        {
            var clientCode = e.Value?.ToString()?.ToUpper() ?? string.Empty;
            var client = Clients.FirstOrDefault(c => c.ClientCode == clientCode);

            SelectedToClient = new KeyValuePair<string, string>(clientCode, client?.ClientName ?? string.Empty);
        }

        public async Task OnCancelModalClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnApplyModalClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center");
            parameters.Add("ModalDialogClass", "modal-md");
            parameters.Add("ToClient", Clients.FirstOrDefault(c => c.ClientCode == SelectedToClient.Key));
            parameters.Add("SelectedFromClient", SelectedFromClient);
            parameters.Add("OnCopyClientSettingsCallback", OnCopyClientSettingsCallback);

            var options = new ModalOptions();
            ModalService.Open<ConfirmCopyClientSettingsModal>($"Copy Client Settings", parameters);
        }
        public void Dispose()
        {

        }
        #endregion

        #region Utilities
        private async Task<List<ClientInformationModel>> LoadClients()
        {
            var clients = new List<ClientInformationModel>();
            clients = (await TradingService.GetClientList()).Result;
            if (clients != null)
            {
                clients = UserModel.ClientCodes.Any() ? clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList() : new List<ClientInformationModel>();
            }

            return clients?.ToList() ?? new List<ClientInformationModel>();
        }
        #endregion
    }
}
