using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.TopFixtures
{
    public partial class Filter : IAsyncDisposable, IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [Parameter] public EventCallback<TopFixtureCallback> OnInitCallback { get; set; }
        [Parameter] public EventCallback<string> OnSearchCallback { get; set; }
        protected SearchFormModel SearchForm { get; set; }
        public List<SelectListItemModel<ClientResponse.Client>> Clients { get; set; }
        public string ClientId { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Clients = new List<SelectListItemModel<ClientResponse.Client>>();

                SearchForm = new SearchFormModel()
                {
                    SearchText = string.Empty
                };
                
                ClientId = string.Empty;

                var clients = (await BackOfficeService.GetClients()).Result;
                if(clients != null)
                {

                    if (UserModel.ClientCodes.Any())
                    {
                        clients.Clients = clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();
                    }

                    Clients = clients.Clients.Select(s => new SelectListItemModel<ClientResponse.Client>
                    {
                        Value = s,
                        Name = s.ClientName
                    }).ToList();

                }

                if (!Clients.Any())
                    Clients.Add(new SelectListItemModel<ClientResponse.Client>
                    {
                        Value = new ClientResponse.Client(),
                        Name = "No Clients Available"
                    });
                else
                    Clients.Insert(0, new SelectListItemModel<ClientResponse.Client> { Value = new ClientResponse.Client(), Name = "Choose client..." });

                var selectedClients = Clients.Where(c => !string.IsNullOrEmpty(c.Value.ClientCode)).Select(s => s.Value).ToList();
                await OnInitCallback.InvokeAsync(new TopFixtureCallback
                {
                    SelectedClients = selectedClients,
                    SearchText = SearchForm.SearchText
                });
            }
            catch (Exception)
            {
            }
        }

        public async void OnClientChangedEvent(ChangeEventArgs e)
        {
            ClientId = e.Value.ToString();

            var selectedClients = Clients.Where(c => !string.IsNullOrEmpty(c.Value.ClientCode)).Select(s => s.Value).ToList();
            if(ClientId != "" && ClientId != "0")
                selectedClients = Clients.Where(c => c.Value.ClientId.ToString() == ClientId).Select(s => s.Value).ToList();

            await OnInitCallback.InvokeAsync(new TopFixtureCallback
            {
                SelectedClients = selectedClients,
                SearchText = SearchForm.SearchText
            });
        }

        public async Task OnInputSearchEvent()
        {
            await OnSearchCallback.InvokeAsync(SearchForm.SearchText);
        }

        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
