using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Diagnostics;

namespace Backoffice.Web.Components.SBC.TopLeagues
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }

        [Parameter] public EventCallback<TopLeaguesFilterModel> OnInitCallback { get; set; }
        [Parameter] public EventCallback<TopLeaguesFilterModel> OnFilterCallback { get; set; }

        protected SearchFormModel SearchForm { get; set; }
        public List<SelectListItemWithCheckboxModel<string>>? Clients { get; set; }
        public string SelectedSportId { get; set; }
        public List<SelectListItemModel<string>> Sports { get; set; }
        public TopLeaguesFilterModel TopLeaguesFilterModel { get; set; }

        public int SelectedClientCount { get; set; }
        public string SelectedClientName { get; set; }

        //private TopLeaguesFilterModel DefaultFilter { get; set; }

        private bool _init = true;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Sports = new List<SelectListItemModel<string>>();
            TopLeaguesFilterModel = new TopLeaguesFilterModel();
            //DefaultFilter = new TopLeaguesFilterModel();

            SearchForm = new SearchFormModel()
            {
                SearchText = string.Empty,
                IsReset = false
            };

            SetClients();

            var sports = (await BackofficeService.GetSportsAsync()).Result;

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

        protected override async Task OnParametersSetAsync()
        {
            if (!_init && !TopLeaguesFilterModel.Clients.Any())
            {
                SetClients();

                if (TopLeaguesFilterModel.Clients.Any())
                    await OnInitCallback.InvokeAsync(TopLeaguesFilterModel);
            }

            _init = false;
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
        public void ClearSearch()
        {
            SearchForm.SearchText = string.Empty;
            SearchForm.IsReset = false;
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

        #region Utilities
        private void SetClients()
        {
            if (_userState.Model.SportsbookClients != null && _userState.Model.SportsbookClients.Any())
            {
                TopLeaguesFilterModel.Clients = _userState.Model.SportsbookClients
                    .Select(s => new TopLeaguesFilterModel.Client
                    {
                        ClientId = s.ClientId,
                        ClientCode = s.ClientCode,
                        ClientName = s.ClientName,
                        Region = s.Region
                    }).ToList();

                Clients = new List<SelectListItemWithCheckboxModel<string>>();
                Clients = TopLeaguesFilterModel.Clients.OrderBy(x => x.ClientName).Select(s => new SelectListItemWithCheckboxModel<string>
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
                SelectedClientName = SelectedClientCount > 0 ? string.Empty : Clients.FirstOrDefault(c => c.Value != "all" && c.IsChecked)?.Name ?? "";

                TopLeaguesFilterModel.ClientIds = string.Join(",", selectedClients);
            }
        }
        #endregion
    }
}