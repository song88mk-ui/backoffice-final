using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Serilog;

namespace Backoffice.Web.Components.SBC.TopFixtures
{
    public partial class CopyModal : IDisposable
    {
        #region Properties   

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter(Name = "UserClientCodes")] public List<string> UserClientCodes { get; set; }

        public List<ClientIdSelectedModel> ClientIdSelectedModel { get; set; }
        public ClientTopFixtureModel.OrderedClientTopFixture TopFixture { get; set; }
        public bool IsSelectAll { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                TopFixture = ModalParameters.TryGet<ClientTopFixtureModel.OrderedClientTopFixture>("TopFixture") ?? new ClientTopFixtureModel.OrderedClientTopFixture();

                var clients = (await BackofficeService.GetClients()).Result;
                if (clients != null)
                {
                    clients.Clients = UserClientCodes != null && UserClientCodes.Any() ? clients.Clients.Where(c => UserClientCodes.Contains(c.ClientCode.ToUpper()) && c.ClientId != TopFixture.ClientIdRef).ToList() : new List<Client>();

                    ClientIdSelectedModel = clients.Clients.Select(s => new ClientIdSelectedModel
                    {
                        IsSelected = false,
                        ClientId = s.ClientId.ToString(),
                        ClientName = s.ClientName
                    }).ToList();
                }

                if (!ClientIdSelectedModel.Any())
                    ClientIdSelectedModel.Add(new ClientIdSelectedModel
                    {
                        IsSelected = false,
                        ClientId = string.Empty,
                        ClientName = TranslationState.GetValue("No Clients Available")
                    });
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
            var clientids = ClientIdSelectedModel.Where(x => x.IsSelected).Select(x => int.Parse(x.ClientId)).ToList();

            if (!clientids.Any())
                return;

            var request = new CopyClientTopFixtureRequest
            {
                ClientIds = clientids,
                TopFixtures = TopFixture.TopFixtures.Select(s => new ClientTopFixtureSetup
                {
                    FixtureId = Convert.ToInt64(s.FixtureId),
                    SortOrder = s.SortOrder
                }).ToList()
            };

            await BackofficeService.CopyClientTopFixtures(request);
            ModalService.Cancel();

            await Modal.OnSaveClickCallback.InvokeAsync(string.Join(",", clientids));
        }

        public void ToggleCheckbox(string clientId)
        {
            foreach (var item in ClientIdSelectedModel)
            {
                if (item.ClientId == clientId)
                    item.IsSelected = !item.IsSelected;
            }
        }

        public void RemoveClient(string clientId)
        {
            var item = ClientIdSelectedModel.FirstOrDefault(x => x.ClientId == clientId);
            if (item != null)
                ClientIdSelectedModel.Remove(item);
        }

        public void SelectAllToggleCheckbox(bool isSelectAll)
        {
            ClientIdSelectedModel.ForEach(x => x.IsSelected = isSelectAll);
            //ClientIdErrorMsg = string.IsNullOrWhiteSpace(ClientIdErrorMsg) ? "Select client to proceed" : string.Empty;
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
