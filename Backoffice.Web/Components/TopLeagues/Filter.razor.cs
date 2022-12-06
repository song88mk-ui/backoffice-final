using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.TopLeagues
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
        public List<SelectListItemModel<int>> Sports { get; set; }
        public string ClientId { get; set; }
        public int SportId { get; set; }
        #endregion

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Clients = new List<SelectListItemModel<string>>();
                Sports = new List<SelectListItemModel<int>>();

                SearchForm = new SearchFormModel()
                {
                    SearchText = string.Empty
                };

                var clients = (await BackOfficeService.GetClients()).Result;
                if (clients != null)
                {
                    if (UserModel.ClientCodes.Any())
                         clients.Clients = clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();

                    Clients = clients.Clients.Select(s => new SelectListItemModel<string>
                    {
                        Value = s.ClientId.ToString(),
                        Name = s.ClientName
                    }).ToList();
                }

                if (!Clients.Any())
                    Clients.Add(new SelectListItemModel<string>
                    {
                        Value = "",
                        Name = "No Clients Available"
                    });

                var sports = (await BackOfficeService.GetSportsAsync("en", "hk")).Result;
                if (sports != null)
                {
                    Sports = sports.Sports.Select(s => new SelectListItemModel<int>
                    {
                        Value = s.SportId,
                        Name = s.SportName
                    }).ToList();
                }

                if (!Sports.Any())
                    Sports.Add(new SelectListItemModel<int>
                    {
                        Value = 0,
                        Name = "No Sports Available"
                    });

                await OnInitCallback.InvokeAsync("");
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
        
        public async void OnSportsChangedEvent(ChangeEventArgs e)
        {
            SportId = (int)e.Value;
            await OnInitCallback.InvokeAsync(SportId.ToString());
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
    }
}