using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Backoffice.Web.Services;


namespace Backoffice.Web.Components.OddsEngine.Monitoring
{
    public partial class Filter : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }

        [Parameter] public EventCallback<MonitoringCallbackModel> OnInitCallback { get; set; }
        [Parameter] public EventCallback<List<SelectListItemWithCheckboxModel<string>>> OnFilterClientCallback { get; set; }
        [Parameter] public int SportId { get; set; }
        [Parameter] public long LeagueId { get; set; }
        #endregion

        #region Fields
        private string _region = "hk";
        private int _sportId;
        private long _leagueId;
        private SearchFormModel _searchForm;
        private List<MainMarketTemplateResponse> _topMarketTemplates;
        private List<SelectListItemWithCheckboxModel<string>>? _clientFilter;
        private string _marketTemplateId = string.Empty;
        private int _selectedClientCount;
        private string _selectedClientName;
        private LanguageCode _languageCode;
        private bool _init = true;

        private string _clientReference;
        private List<ClientInformationModel> _clientListReference = new List<ClientInformationModel>();
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _topMarketTemplates = new List<MainMarketTemplateResponse>();
            _searchForm = new SearchFormModel();
            _clientFilter = new List<SelectListItemWithCheckboxModel<string>>();

            _tradingService.Region = _region.ToUpper();
        }

        protected override async Task OnParametersSetAsync()
        {
            var tempClientReference = string.Join(',', _userState.Model.TradingClients.OrderBy(c => c.ClientCode).Select(s => s.ClientCode.Trim()).ToList());

            if (_userState.Model.TradingClients != null && _userState.Model.TradingClients.Any() && !_clientListReference.Any())
            {
                _clientListReference = new List<ClientInformationModel>(_userState.Model.TradingClients);
                _init = true;
            }

            if (_init || _clientReference != tempClientReference)
            {
                _clientFilter = null;

                _clientReference = tempClientReference;
                if (_clientListReference != null)
                {
                    _clientFilter = _userState.Model.TradingClients?.Select(c => new SelectListItemWithCheckboxModel<string> { Value = c.ClientCode, Name = c.ClientName, IsChecked = true }).ToList() ?? new List<SelectListItemWithCheckboxModel<string>>();

                    if (_clientFilter.Any())
                    {
                        _clientFilter.Insert(0, new SelectListItemWithCheckboxModel<string>
                        {
                            Value = "all",
                            Name = "All Clients",
                            IsChecked = true
                        });
                    }
                }

                _selectedClientCount = _clientFilter.Where(c => c.Value != "all").Count();
                _selectedClientName = _selectedClientCount > 0 ? string.Empty : _clientFilter?.FirstOrDefault(c => c.Value != "all" && c.IsChecked)?.Name ?? "";

                await OnFilterClientCallback.InvokeAsync(_clientFilter);
            }


            if (_sportId != SportId || _leagueId != LeagueId || _languageCode != _translationState.ActiveLanguageCode)
            {
                _sportId = SportId;
                _leagueId = LeagueId;
                _languageCode = _translationState.ActiveLanguageCode;
                _searchForm.SearchText = string.Empty;

                var topMarketsResponse = await _backofficeService.GetTopMarkets(_sportId, _languageCode);
                _topMarketTemplates = topMarketsResponse.Result;
                _marketTemplateId = _topMarketTemplates?.FirstOrDefault()?.MainMarketTemplateId ?? string.Empty;

                if (!string.IsNullOrEmpty(_marketTemplateId))
                {
                    await OnInitCallback.InvokeAsync(new MonitoringCallbackModel
                    {
                        SearchText = _searchForm.SearchText,
                        SelectedMarketTemplateId = _marketTemplateId
                    });
                }
            }

            _init = false;
        }

        private async void OnMarketChangedEvent(ChangeEventArgs e)
        {
            _marketTemplateId = e?.Value?.ToString() ?? string.Empty;

            await OnInitCallback.InvokeAsync(new MonitoringCallbackModel
            {
                SelectedMarketTemplateId = _marketTemplateId,
                SearchText = _searchForm.SearchText
            });
        }

        private async Task OnInputSearchEvent()
        {
            if (!string.IsNullOrEmpty(_searchForm.SearchText) && _searchForm.SearchText.Length == 1)
                return;

            await OnInitCallback.InvokeAsync(new MonitoringCallbackModel
            {
                SearchText = _searchForm.SearchText,
                SelectedMarketTemplateId = _marketTemplateId
            });
        }

        public async Task OnClientCheckboxClickEvent(Dictionary<string, string> items)
        {
            _selectedClientName = string.Empty;
            _selectedClientCount = items.Count;

            if (_selectedClientCount == 1)
                _selectedClientName = items.FirstOrDefault().Value;

            await OnFilterClientCallback.InvokeAsync(_clientFilter);
        }

        public async Task OnSwitchAllClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-dialog-centered modal-sm");
            var options = new ModalOptions();

            ModalService.Open<SwitchAllFixturesModal>("SWITCH ALL FIXTURES", parameters, options);
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities
        #endregion
    }
}
