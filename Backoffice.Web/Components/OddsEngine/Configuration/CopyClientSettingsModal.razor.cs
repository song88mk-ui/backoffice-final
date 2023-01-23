using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class CopyClientSettingsModal : IDisposable
    {
        #region Properties
        [Inject] private ITradingService TradingService { get; set; }
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [Inject] private IModalService ModalService { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        private List<SelectListItemModel<string>> Clients { get; set; }
        private List<SelectListItemModel<string>> FromClients { get; set; }
        private List<SelectListItemModel<string>> ToClients { get; set; }

        private KeyValuePair<string,string> SelectedFromClient { get; set; }
        private KeyValuePair<string, string> SelectedToClient { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Clients = new List<SelectListItemModel<string>>();
            FromClients = new List<SelectListItemModel<string>>();
            ToClients = new List<SelectListItemModel<string>>();

            await Initialize();
        }

        public async Task Initialize()
        {
            Clients = await LoadClients();
            SelectedFromClient = new KeyValuePair<string, string>(Clients[0].Value, Clients[0].Name);
            SelectedToClient = new KeyValuePair<string, string>(Clients[1].Value, Clients[1].Name);

            FromClients = Clients.Where(x => x.Value != SelectedToClient.Key).ToList();
            ToClients = Clients.Where(x => x.Value != SelectedFromClient.Key).ToList();
        }

        public async Task OnFromClientChangedEvent(ChangeEventArgs e)
        {
            string ClientId = e.Value.ToString();

            SelectedFromClient = new KeyValuePair<string, string>(e.Value.ToString(), Clients.FirstOrDefault(x => x.Value == ClientId).Name);
            ToClients = Clients.Where(x => x.Value != SelectedFromClient.Key).ToList();
        }

        public async Task OnToClientChangedEvent(ChangeEventArgs e)
        {
            string ClientId = e.Value.ToString();

            SelectedToClient = new KeyValuePair<string, string>(e.Value.ToString(), Clients.FirstOrDefault(x => x.Value == ClientId).Name);
            FromClients = Clients.Where(x => x.Value != SelectedToClient.Key).ToList();
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
            parameters.Add("SelectedFromClient", SelectedFromClient);
            parameters.Add("SelectedToClient", SelectedToClient);

            var options = new ModalOptions();
            ModalService.Open<ConfirmCopyClientSettingsModal>($"Copy Client Settings", parameters);
        }
        public void Dispose()
        {
            
        }
        #endregion

        #region Utilities
        private async Task<List<SelectListItemModel<string>>> LoadClients()
        {
            ClientModel clients = new ClientModel();
            clients = (await BackOfficeService.GetClients()).Result;
            if (clients != null)
            {

                if (UserModel.ClientCodes.Any())
                {
                    clients.Clients = clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();
                }
            }

            return clients.Clients.Select(x => new SelectListItemModel<string>()
            {
                Name = x.ClientName,
                Value = x.ClientCode
            }).ToList();
        }
        #endregion
    }
}
