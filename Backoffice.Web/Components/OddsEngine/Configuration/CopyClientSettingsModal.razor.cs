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

        [CascadingParameter(Name = "UserClientCodes")] public List<string>? UserClientCodes { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        private EventCallback<AlertModel> OnCopyClientSettingsCallback { get; set; }

        private List<ClientInformationModel> Clients { get; set; }
        private List<SelectListItemModel<string>> FromClients { get; set; }
        private List<SelectListItemModel<string>> ToClients { get; set; }
        private string Region { get; set; }

        private KeyValuePair<string, string> SelectedFromClient { get; set; }
        private KeyValuePair<string, string> SelectedToClient { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Clients = new List<ClientInformationModel>();
            FromClients = new List<SelectListItemModel<string>>();
            ToClients = new List<SelectListItemModel<string>>();

            OnCopyClientSettingsCallback = ModalParameters.TryGet<EventCallback<AlertModel>>("OnCopyClientSettingsCallback");
            Region = ModalParameters.TryGet<string>("Region").ToUpper();
            TradingService.Region = Region.ToUpper();

            await Initialize();
        }

        public async Task Initialize()
        {
            Clients = await LoadClients();
            SelectedFromClient = new KeyValuePair<string, string>(Clients[0].ClientCode, Clients[0].ClientName);
            SelectedToClient = new KeyValuePair<string, string>(Clients[1].ClientCode, Clients[1].ClientName);

            FromClients = Clients.Where(x => x.ClientCode != SelectedToClient.Key).Select(x => new SelectListItemModel<string> { Value = x.ClientCode, Name = x.ClientName }).ToList();
            ToClients = Clients.Where(x => x.ClientCode != SelectedFromClient.Key).Select(x => new SelectListItemModel<string> { Value = x.ClientCode, Name = x.ClientName }).ToList();
        }

        public async Task OnFromClientChangedEvent(ChangeEventArgs e)
        {
            string ClientId = e.Value.ToString();

            SelectedFromClient = new KeyValuePair<string, string>(e.Value.ToString(), Clients.FirstOrDefault(x => x.ClientCode == ClientId).ClientName);
            ToClients = Clients.Where(x => x.ClientCode != SelectedFromClient.Key).Select(x => new SelectListItemModel<string> { Value = x.ClientCode, Name = x.ClientName }).ToList();
        }

        public async Task OnToClientChangedEvent(ChangeEventArgs e)
        {
            string ClientId = e.Value.ToString();

            SelectedToClient = new KeyValuePair<string, string>(e.Value.ToString(), Clients.FirstOrDefault(x => x.ClientCode == ClientId).ClientName);
            FromClients = Clients.Where(x => x.ClientCode != SelectedToClient.Key).Select(x => new SelectListItemModel<string> { Value = x.ClientCode, Name = x.ClientName }).ToList();
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
            parameters.Add("SelectedToClient", SelectedToClient);
            parameters.Add("OnCopyClientSettingsCallback", OnCopyClientSettingsCallback);
            parameters.Add("Region", Region);

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
                clients = UserClientCodes != null && UserClientCodes.Any() ? clients.Where(c => UserClientCodes.Contains(c.ClientCode.ToUpper())).ToList() : new List<ClientInformationModel>();
            }

            return clients?.ToList() ?? new List<ClientInformationModel>();
        }
        #endregion
    }
}
