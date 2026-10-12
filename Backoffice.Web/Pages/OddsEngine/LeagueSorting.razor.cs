using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.OddsEngine.LeagueSorting;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.LeagueSorting;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.ComponentModel;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class LeagueSorting : IDisposable
    {
        #region Fields

        private const int PageSize = 25;

        private List<BreadcrumbModel> _breadcrumbs = new List<BreadcrumbModel>();
        private PageHeaderModel _pageHeaderModel = new PageHeaderModel();
        private AlertModel _alertModel;

        private bool _isPreLoader = true;
        private bool _canEdit;

        private List<SelectListItemModel<string>> _sports = new List<SelectListItemModel<string>>();
        private string _selectedSportId;
        private string _selectedSportName;

        // Index 0 is the "all clients" entry that SelectWithCheckbox expects.
        private List<SelectListItemWithCheckboxModel<string>> _clientFilter = new List<SelectListItemWithCheckboxModel<string>>();
        private string _search = string.Empty;

        private List<ClientOddstableLeagueOrderListResponse> _orders = new List<ClientOddstableLeagueOrderListResponse>();
        private List<LeagueSortingClient> _allClients = new List<LeagueSortingClient>();
        private List<LeagueSortingClient> _clients = new List<LeagueSortingClient>();
        private MetaData _metaData = new MetaData { CurrentPage = 1, PageSize = PageSize, TotalCount = 0, TotalPages = 1 };

        #endregion

        #region Properties

        private string Search
        {
            get => _search;
            set
            {
                _search = value ?? string.Empty;
                ApplyFilters(resetPage: true);
            }
        }

        private int SelectedClientCount => _clientFilter.Count(c => c.Value != "all" && c.IsChecked);

        private string ClientFilterLabel
        {
            get
            {
                var count = SelectedClientCount;

                if (count == _clientFilter.Count - 1)
                    return $"{_translationState.GetValue("All Clients")} ({count})";

                return count == 1
                    ? _clientFilter.First(c => c.Value != "all" && c.IsChecked).Name
                    : $"{_translationState.GetValue("Clients")} ({count})";
            }
        }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            if (PreRenderService.IsPreRendering)
                return;

            PageState.Page = this;

            _breadcrumbs = new List<BreadcrumbModel>
            {
                new BreadcrumbModel { Name = "Sports Settings", Url = "/hk/odds-engine/sport-settings", IsCurrentPage = false },
                new BreadcrumbModel { Name = "League Sorting", Url = "/hk/odds-engine/league-sorting", IsCurrentPage = true }
            };

            _pageHeaderModel = new PageHeaderModel
            {
                Title = "League Sorting",
                ShowLanguageDropDown = false
            };

            _canEdit = _userState.Model.Claims?.Any(x =>
                x.Type == $"{SportsbookClientPermission.SBC_OE_LeagueSorting.ToString().Replace("_", ".")}"
                && x.Value == $"{(int)Access.Edit}") ?? false;

            _userState.PropertyChanged += OnUserStateChanged;

            InitializeClientFilter();

            await LoadSports();

            if (!string.IsNullOrEmpty(_selectedSportId))
                await LoadLeagueOrders();
            else
                _isPreLoader = false;
        }

        public async Task OnSportChangedEvent(ChangeEventArgs e)
        {
            _selectedSportId = e.Value?.ToString() ?? string.Empty;
            _selectedSportName = _sports.FirstOrDefault(s => s.Value == _selectedSportId)?.Name ?? string.Empty;
            _metaData.CurrentPage = 1;

            await LoadLeagueOrders();
        }

        public void OnClientCheckboxEvent(Dictionary<string, string> items)
        {
            ApplyFilters(resetPage: true);
        }

        public void OnClearSearchEvent()
        {
            Search = string.Empty;
        }

        public void SelectPage(int page)
        {
            _metaData.CurrentPage = page;
            ApplyFilters(resetPage: false);
        }

        public void OnEditClickEvent(LeagueSortingClient client)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalDialogClass", "modal-xl");
            parameters.Add("ClientCode", client.ClientCode);
            parameters.Add("SportId", int.Parse(_selectedSportId));
            parameters.Add("SportName", _selectedSportName);
            parameters.Add("CurrentLeagues", client.Leagues);

            _modalService.Open<EditModal>($"Set the League Sorting of {{{client.ClientName}}} ({{{_selectedSportName}}})", parameters, new ModalOptions());
        }

        public void OnCopyClickEvent(LeagueSortingClient client)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalDialogClass", "modal-xl");
            parameters.Add("FromClientCode", client.ClientCode);
            parameters.Add("FromClientName", client.ClientName);
            parameters.Add("SportId", int.Parse(_selectedSportId));
            parameters.Add("SportName", _selectedSportName);
            parameters.Add("Leagues", client.Leagues);

            _modalService.Open<CopyModal>($"Copy {{{client.ClientName}}} League Sorting Settings ({{{_selectedSportName}}}) To", parameters, new ModalOptions());
        }

        // The leagues are already on the page, so the export needs no request. It works for View access too.
        public async Task OnExportClickEvent(LeagueSortingClient client)
        {
            var csv = LeagueSortingCsvHelper.BuildCsv(client.ClientCode, int.Parse(_selectedSportId), client.Leagues);
            var fileName = LeagueSortingCsvHelper.BuildFileName(client.ClientCode, _selectedSportName, DateTime.UtcNow);

            await _jsRuntime.InvokeVoidAsync("downloadFileFromText", fileName, csv, "text/csv");
        }

        public void OnImportClickEvent(LeagueSortingClient client)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalDialogClass", "modal-xl");
            parameters.Add("ClientCode", client.ClientCode);
            parameters.Add("ClientName", client.ClientName);
            parameters.Add("SportId", int.Parse(_selectedSportId));
            parameters.Add("SportName", _selectedSportName);
            parameters.Add("CurrentLeagueCount", client.Leagues.Count);

            _modalService.Open<ImportModal>($"Import League Sorting of {{{client.ClientName}}} ({{{_selectedSportName}}})", parameters, new ModalOptions());
        }

        public void OnLogsClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalDialogClass", "modal-xl");

            _modalService.Open<LogsModal>("League Sorting Logs", parameters, new ModalOptions());
        }

        public async Task OnSavedEvent()
        {
            await LoadLeagueOrders();
        }

        public void Dispose()
        {
            _userState.PropertyChanged -= OnUserStateChanged;
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        private void InitializeClientFilter()
        {
            var clients = _userState.Model.TradingClients;

            if (clients == null || !clients.Any())
                return;

            _clientFilter = clients
                .OrderBy(c => c.ClientName)
                .Select(c => new SelectListItemWithCheckboxModel<string> { Value = c.ClientCode, Name = c.ClientName, IsChecked = true })
                .ToList();

            _clientFilter.Insert(0, new SelectListItemWithCheckboxModel<string> { Value = "all", Name = "All Clients", IsChecked = true });
        }

        // The layout loads the user's clients on its own, so they can arrive after this page has started.
        private async void OnUserStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_clientFilter.Any() || _userState.Model.TradingClients == null || !_userState.Model.TradingClients.Any())
                return;

            InitializeClientFilter();
            RebuildClients();

            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadSports()
        {
            var response = await _backofficeService.GetSportLookup(_translationState.ActiveLanguageCode.ToString());
            var sports = response?.Result?.SportLookup;

            if (response?.StatusCode != StatusCode.Success || sports == null || !sports.Any())
            {
                _alertModel = new AlertModel { Status = AlertStatus.Error, Message = "No sports available." };
                return;
            }

            _sports = sports
                .Select(s => new SelectListItemModel<string> { Value = s.SportId.ToString(), Name = s.SportName.TrimEnd() })
                .ToList();

            var defaultSport = _sports.FirstOrDefault(s => s.Value == ((int)Backoffice.Shared.Enums.Sport.Soccer).ToString()) ?? _sports.First();

            _selectedSportId = defaultSport.Value;
            _selectedSportName = defaultSport.Name;
        }

        private async Task LoadLeagueOrders()
        {
            _isPreLoader = true;
            _alertModel = new AlertModel();
            await InvokeAsync(StateHasChanged);

            var response = await _backofficeService.GetClientOddstableLeagueOrderList(new ClientOddstableLeagueOrderListRequest
            {
                SportId = int.Parse(_selectedSportId)
            });

            if (response.StatusCode == StatusCode.Success && response.Result != null)
            {
                _orders = response.Result;
            }
            else
            {
                _orders = new List<ClientOddstableLeagueOrderListResponse>();
                _alertModel = new AlertModel { Status = AlertStatus.Error, Message = response.Message ?? "Unable to load the league sorting." };
            }

            RebuildClients();
            _isPreLoader = false;
        }

        private void RebuildClients()
        {
            _allClients = LeagueSortingHelper.BuildClientRows(
                (_userState.Model.TradingClients ?? new List<PlatformClientResponse>()).Select(c => (c.ClientCode, c.ClientName)),
                _orders);

            ApplyFilters(resetPage: false);
        }

        private void ApplyFilters(bool resetPage)
        {
            var selectedCodes = _clientFilter
                .Where(c => c.Value != "all" && c.IsChecked)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var filtered = LeagueSortingHelper.Filter(_allClients, selectedCodes, _search);

            _metaData.PageSize = PageSize;
            _metaData.TotalCount = filtered.Count;
            _metaData.TotalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));

            if (resetPage || _metaData.CurrentPage > _metaData.TotalPages || _metaData.CurrentPage < 1)
                _metaData.CurrentPage = 1;

            _clients = filtered
                .Skip((_metaData.CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        #endregion
    }
}
