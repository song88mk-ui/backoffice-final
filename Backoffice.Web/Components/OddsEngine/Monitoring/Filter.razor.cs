using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Monitoring
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [Parameter] public EventCallback<MonitoringCallbackModel> OnInitCallback { get; set; }
        [Parameter] public EventCallback<List<SelectListItemWithCheckboxModel<string>>> OnFilterClientCallback { get; set; }
        [Parameter] public int SportId { get; set; }
        #endregion

        #region Fields
        private string _region = "hk";
        private int _sportId;
        private SearchFormModel _searchForm;
        private List<MainMarketTemplateResponse> _topMarketTemplates;
        private List<SelectListItemWithCheckboxModel<string>> _clientFilter;
        private string _marketTemplateId = string.Empty;
        private int _selectedClientCount;
        private string _selectedClientName;
        private LanguageCode _languageCode;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _topMarketTemplates = new List<MainMarketTemplateResponse>();
            _searchForm = new SearchFormModel();
            _clientFilter = new List<SelectListItemWithCheckboxModel<string>>();

            TradingService.Region = _region.ToUpper();

            var clientsResponse = await TradingService.GetClientList();
            var clients = clientsResponse.Result;

            if (clients != null)
            {
                _clientFilter = UserModel.ClientCodes.Any() 
                    ? clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).Select(c => new SelectListItemWithCheckboxModel<string> { Value = c.ClientCode, Name = c.ClientName, IsChecked = true }).ToList() 
                    : new();

                _clientFilter.Insert(0, new SelectListItemWithCheckboxModel<string>
                {
                    Value = "all",
                    Name = "All Clients",
                    IsChecked = true
                });
            }
            else
            {
                _clientFilter.Add(new SelectListItemWithCheckboxModel<string>
                {
                    Value = "all",
                    Name = "All Clients",
                    IsChecked = true
                });
            }

            _selectedClientCount = _clientFilter.Count;
            _selectedClientName = _selectedClientCount > 0 ? string.Empty : _clientFilter.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;

            await OnFilterClientCallback.InvokeAsync(_clientFilter);
        }

        protected override async Task OnParametersSetAsync()
        {
            if (_sportId != SportId || _languageCode != _translationState.ActiveLanguageCode)
            {
                _sportId = SportId;
                _languageCode = _translationState.ActiveLanguageCode;

                var topMarketsResponse = await BackofficeService.GetTopMarkets(_sportId, _languageCode);
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

        public void Dispose()
        {
        }

        #endregion

        #region Utilities
        #endregion
    }
}
