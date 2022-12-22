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
        [Parameter] public EventCallback<TopLeaguesFilterModel> OnInitCallback { get; set; }
        [Parameter] public EventCallback<TopLeaguesFilterModel> OnFilterCallback { get; set; }
        protected SearchFormModel SearchForm { get; set; }
        public List<Models.SelectListItemModel<string>> Clients { get; set; }
        public List<Models.SelectListItemModel<string>> Sports { get; set; }
        public TopLeaguesFilterModel TopLeaguesFilterModel { get; set; }
        #endregion

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Clients = new List<Models.SelectListItemModel<string>>();
                Sports = new List<Models.SelectListItemModel<string>>();
                TopLeaguesFilterModel = new TopLeaguesFilterModel();

                SearchForm = new SearchFormModel()
                {
                    SearchText = string.Empty
                };

                var clients = (await BackOfficeService.GetClients()).Result;
                if (clients != null)
                {
                    if (UserModel.ClientCodes.Any())
                         clients.Clients = clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();

                    Clients = clients.Clients.Select(s => new Models.SelectListItemModel<string>
                    {
                        Value = s.ClientId.ToString(),
                        Name = s.ClientName
                    }).ToList();
                }

                if (!Clients.Any())
                    Clients.Add(new Models.SelectListItemModel<string>
                    {
                        Value = "",
                        Name = "No Clients Available"
                    });

                var sports = (await BackOfficeService.GetSportsAsync("en")).Result;
                if (sports != null)
                {
                    Sports = sports.Sports.Select(s => new Models.SelectListItemModel<string>
                    {
                        Value = s.SportId.ToString(),
                        Name = s.SportName
                    }).ToList();
                }

                if (!Sports.Any())
                    Sports.Add(new Models.SelectListItemModel<string>
                    {
                        Value = "0",
                        Name = "No Sports Available"
                    });

                await OnInitCallback.InvokeAsync();
            }
            catch (Exception)
            {
            }
        }
        
        public async void OnClientChangedEvent(ChangeEventArgs e)
        {
            TopLeaguesFilterModel.ClientId = e.Value.ToString();
            await OnFilterCallback.InvokeAsync(TopLeaguesFilterModel);
        }
        
        public async void OnSportsChangedEvent(ChangeEventArgs e)
        {
            TopLeaguesFilterModel.SportId = e.Value.ToString();
            TopLeaguesFilterModel.SportName = Sports.FirstOrDefault(x => x.Value == TopLeaguesFilterModel.SportId).Name;
            await OnFilterCallback.InvokeAsync(TopLeaguesFilterModel);
        }
        
        public async Task OnInputSearchEvent()
        {
            TopLeaguesFilterModel.Search = SearchForm.SearchText;
            await OnFilterCallback.InvokeAsync(TopLeaguesFilterModel);
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