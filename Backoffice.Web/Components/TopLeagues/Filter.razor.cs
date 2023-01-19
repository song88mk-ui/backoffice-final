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
        public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        public string SelectedSportId { get; set; }
        public List<SelectListItemModel<string>> Sports { get; set; }
        public TopLeaguesFilterModel TopLeaguesFilterModel { get; set; }
        public int SelectedClientCount { get; set; }
        public string SelectedClientName { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Clients = new List<SelectListItemWithCheckboxModel<string>>();
            Sports = new List<SelectListItemModel<string>>();
            TopLeaguesFilterModel = new TopLeaguesFilterModel();

            SearchForm = new SearchFormModel()
            {
                SearchText = string.Empty
            };

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

                Clients = TopLeaguesFilterModel.Clients.Select(s => new SelectListItemWithCheckboxModel<string>
                {
                    Value = s.ClientId.ToString(),
                    Name = s.ClientName,
                    IsChecked = true
                }).ToList();

                Clients.Insert(0, new SelectListItemWithCheckboxModel<string>
                {
                    Value = "all",
                    Name = "All Clients",
                    IsChecked = true
                });

                var selectedClients = Clients.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

                SelectedClientCount = selectedClients.Count;
                TopLeaguesFilterModel.ClientIds = string.Join(",", selectedClients);
            }

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

        public async Task OnClientCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedClientName = string.Empty;
            SelectedClientCount = items.Count;
            TopLeaguesFilterModel.ClientIds = string.Join(",", items.Select(s => s.Key));

            if (SelectedClientCount == 1)
            {
                SelectedClientName = items.FirstOrDefault().Value;
            }

            await OnFilterCallback.InvokeAsync(TopLeaguesFilterModel);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}