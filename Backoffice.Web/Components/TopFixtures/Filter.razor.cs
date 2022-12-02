using Backoffice.Shared.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.TopFixtures
{
    public partial class Filter : IAsyncDisposable, IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Parameter] public EventCallback<string> OnInitCallback { get; set; }
        [Parameter] public EventCallback<string> OnSearchCallback { get; set; }
        protected SearchFormModel SearchForm { get; set; }
        public List<SelectListItemModel<string>> Clients { get; set; }
        public string ClientCode { get; set; }
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
                    Clients = clients.Clients.Select(s => new SelectListItemModel<string>
                    {
                        Value = s.Brand,
                        Name = s.BusinessName
                    }).ToList();
                }

                if (!Clients.Any())
                    Clients.Add(new SelectListItemModel<string>
                    {
                        Value = "",
                        Name = "No Clients Available"
                    });

            }
            catch (Exception)
            {
            }
        }

        public async void OnClientChangedEvent(ChangeEventArgs e)
        {
            ClientCode = e.Value.ToString();
            await OnInitCallback.InvokeAsync(ClientCode);
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
