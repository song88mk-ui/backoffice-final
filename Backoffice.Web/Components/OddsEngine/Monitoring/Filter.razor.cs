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

        [Inject] public IBackofficeService BackOfficeService { get; set; }
        [Parameter] public EventCallback<MonitoringCallbackModel> OnInitCallback { get; set; }
        [Parameter] public int SportId { get; set; }
        #endregion

        #region Fields
        private int _sportId;
        private SearchFormModel _searchForm;
        private List<MainMarketTemplateResponse> _topMarketTemplates;
        private string _marketTemplateId = string.Empty;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _topMarketTemplates = new List<MainMarketTemplateResponse>();
            _searchForm = new SearchFormModel();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (_sportId != SportId)
            {
                _sportId = SportId;

                var topMarketsResponse = await BackOfficeService.GetTopMarkets(_sportId);
                _topMarketTemplates = topMarketsResponse.Result;
                _marketTemplateId = _topMarketTemplates.FirstOrDefault()?.MainMarketTemplateId ?? string.Empty;

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

        public void Dispose()
        {
        }

        #endregion

        #region Utilities
        #endregion
    }
}
