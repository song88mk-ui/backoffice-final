using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.TopLeagues
{
    public partial class Filter : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        [Parameter] public EventCallback<TopLeaguesFilterModel> OnInitCallback { get; set; }
        [Parameter] public EventCallback<TopLeaguesFilterModel> OnFilterCallback { get; set; }

        protected SearchFormModel SearchForm { get; set; }
        public string SelectedClientId { get; set; }
        public List<SelectListItemModel<string>> Clients { get; set; }
        public string SelectedSportId { get; set; }
        public List<SelectListItemModel<string>> Sports { get; set; }
        public TopLeaguesFilterModel TopLeaguesFilterModel { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Clients = new List<SelectListItemModel<string>>();
            Sports = new List<SelectListItemModel<string>>();
            TopLeaguesFilterModel = new TopLeaguesFilterModel();

            SearchForm = new SearchFormModel()
            {
                SearchText = string.Empty
            };

            SelectedClientId = string.Empty;

            var clients = (await BackOfficeService.GetClients()).Result;

            if (clients != null && clients.Clients.Any())
            {
                if (UserModel.ClientCodes.Any())
                    clients.Clients = clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();

                TopLeaguesFilterModel.Clients = clients.Clients
                    .Select(s => new TopLeaguesFilterModel.Client
                    {
                        ClientId = s.ClientId,
                        ClientCode = s.ClientCode,
                        ClientName = s.ClientName,
                        Region = s.Region
                    }).ToList();

                Clients = TopLeaguesFilterModel.Clients.Select(s => new SelectListItemModel<string>
                {
                    Value = s.ClientId.ToString(),
                    Name = s.ClientName
                }).ToList();

                Clients.Insert(0, new SelectListItemModel<string>
                {
                    Value = "all",
                    Name = "All Clients"
                });
            }
            else
            {
                Clients.Add(new SelectListItemModel<string>
                {
                    Value = "",
                    Name = "No Clients Available"
                });
            }

            SelectedClientId = Clients.FirstOrDefault().Value;
            TopLeaguesFilterModel.ClientId = SelectedClientId;

            var sports = (await BackOfficeService.GetSportsAsync()).Result;

            if (sports != null && sports.SportLookup.Any())
            {
                Sports = sports.SportLookup.Select(s => new SelectListItemModel<string>
                {
                    Value = s.SportId.ToString(),
                    Name = s.SportName
                }).ToList();

                var sport = Sports.FirstOrDefault(c => c.Value == "1"); // soccer as initial value

                SelectedSportId = sport.Value;

                TopLeaguesFilterModel.SportId = SelectedSportId;
                TopLeaguesFilterModel.SportName = sport.Name;
            }
            else
            {
                Sports.Add(new SelectListItemModel<string>
                {
                    Value = "0",
                    Name = "No Sports Available"
                });

                var sport = Sports.FirstOrDefault();

                SelectedSportId = sport.Value;

                TopLeaguesFilterModel.SportId = SelectedSportId;
                TopLeaguesFilterModel.SportName = sport.Name;
            }

            await OnInitCallback.InvokeAsync(TopLeaguesFilterModel);
        }
        
        public async void OnClientChangedEvent(ChangeEventArgs e)
        {
            SelectedClientId = e.Value.ToString();
            TopLeaguesFilterModel.ClientId = SelectedClientId;

            await OnFilterCallback.InvokeAsync(TopLeaguesFilterModel);
        }
        
        public async void OnSportsChangedEvent(ChangeEventArgs e)
        {
            SelectedSportId = e.Value.ToString();
            TopLeaguesFilterModel.SportId = SelectedSportId;
            TopLeaguesFilterModel.SportName = Sports.FirstOrDefault(c => c.Value == TopLeaguesFilterModel.SportId).Name;

            await OnFilterCallback.InvokeAsync(TopLeaguesFilterModel);
        }
        
        public async Task OnInputSearchEvent()
        {
            TopLeaguesFilterModel.Search = SearchForm.SearchText;

            await OnFilterCallback.InvokeAsync(TopLeaguesFilterModel);
        }
        
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}