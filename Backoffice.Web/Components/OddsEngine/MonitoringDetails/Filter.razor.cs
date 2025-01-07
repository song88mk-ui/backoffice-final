using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.MonitoringDetails
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }

        [Parameter] public EventCallback<string> OnMarketChangedCallback { get; set; }
        [Parameter] public EventCallback OnCloseMarketCallback { get; set; }
        [Parameter] public EventCallback<List<SelectListItemWithCheckboxModel<string>>> OnFilterClientCallback { get; set; }
        [Parameter] public int SportId { get; set; }

        public List<SelectListItemModel<string>> MarketTemplates { get; set; }
        public string SelectedMarketTemplateId { get; set; }

        #endregion

        #region Fields
        private string _region = "hk";
        private List<SelectListItemWithCheckboxModel<string>> _clientFilter;
        private int _selectedClientCount;
        private string _selectedClientName;
        private LanguageCode _languageCode;
        private int _sportId;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _clientFilter = new List<SelectListItemWithCheckboxModel<string>>();
            TradingService.Region = _region.ToUpper();

            var clientsResponse = await TradingService.GetClientList();
            var clients = clientsResponse.Result;

            if (clients != null)
            {
                _clientFilter = _userState.ClientCodes.Any() ?
                    clients.Where(c => _userState.ClientCodes.Contains(c.ClientCode.ToUpper()))
                        .Select(c => new SelectListItemWithCheckboxModel<string> { Value = c.ClientCode, Name = c.ClientName, IsChecked = true }).ToList() : new();

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

            MarketTemplates = new List<SelectListItemModel<string>>();

            _selectedClientCount = _clientFilter.Where(c => c.Value != "all").Count();
            _selectedClientName = _selectedClientCount > 0 ? string.Empty : _clientFilter.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;

            await OnFilterClientCallback.InvokeAsync(_clientFilter);
        }
        protected override async Task OnParametersSetAsync()
        {
            if (_sportId != SportId || _languageCode != _translationState.ActiveLanguageCode)
            {
                _sportId = SportId;
                _languageCode = _translationState.ActiveLanguageCode;

                var mainMarketTemplateResponse = await BackofficeService.GetTopMarkets(_sportId, _languageCode);

                if (mainMarketTemplateResponse.StatusCode == StatusCode.Success)
                {
                    MarketTemplates = mainMarketTemplateResponse.Result.Select(s => new SelectListItemModel<string>
                    {
                        Value = s.MainMarketTemplateId,
                        Name = s.Name
                    }).ToList();
                }

                if (MarketTemplates.Any())
                {
                    SelectedMarketTemplateId = MarketTemplates.FirstOrDefault().Value;
                }

                await OnMarketChangedCallback.InvokeAsync(SelectedMarketTemplateId);
            }
        }

        public async void OnMarketChangedEvent(ChangeEventArgs e)
        {
            SelectedMarketTemplateId = e.Value.ToString();

            await OnMarketChangedCallback.InvokeAsync(SelectedMarketTemplateId);
        }

        public async Task OnCloseMarketsClickEvent()
        {
            await OnCloseMarketCallback.InvokeAsync();
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
    }
}
