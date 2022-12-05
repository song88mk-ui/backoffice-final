using Backoffice.Shared.Models;
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
        [Parameter] public EventCallback<string> OnInitCallback { get; set; }
        [Parameter] public EventCallback<string> OnSearchCallback { get; set; }
        protected SearchFormModel SearchForm { get; set; }
        public List<SelectListItemModel<string>> Clients { get; set; }
        public string ClientId { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Clients = new List<SelectListItemModel<string>>();

                SearchForm = new SearchFormModel()
                {
                    SearchText = string.Empty
                };

                var clients = (await BackOfficeService.GetClients()).Result;
                if(clients != null)
                {

                    if (UserModel.ClientCodes.Any())
                    {
                        clients.Clients = clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.Brand.ToUpper())).ToList();
                    }

                    Clients = clients.Clients.Select(s => new SelectListItemModel<string>
                    {
                        Value = s.ClientId.ToString(),
                        Name = s.BusinessName
                    }).ToList();

                }

                if (!Clients.Any())
                    Clients.Add(new SelectListItemModel<string>
                    {
                        Value = "",
                        Name = "No Clients Available"
                    });

                var clientCodes = string.Join(",", Clients.Where(c => !string.IsNullOrEmpty(c.Value)).Select(s => s.Value).ToList());
                await OnInitCallback.InvokeAsync(clientCodes);
            }
            catch (Exception)
            {
            }
        }

        public async void OnClientChangedEvent(ChangeEventArgs e)
        {
            ClientId = e.Value.ToString();
            await OnInitCallback.InvokeAsync(ClientId);
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
