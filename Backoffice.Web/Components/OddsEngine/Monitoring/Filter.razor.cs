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

        [Inject] public IBackOfficeService BackOfficeService { get; set; }

        [Parameter] public EventCallback<MonitoringCallbackModel> OnInitCallback { get; set; }
        [Parameter] public int SportId { get; set; }
        [Parameter] public int SegmentId { get; set; }
        [Parameter] public int LeagueId { get; set; }

        public SearchFormModel SearchForm { get; set; }
        public List<MainMarketTemplateResponse> TopMarketTemplates { get; set; }

        public string MarketTemplateId { get; set; } = string.Empty;

        #endregion

        #region Fields

        protected int _sportId;
        protected int _segmentId;
        protected int _leagueId;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            TopMarketTemplates = new List<MainMarketTemplateResponse>();
            SearchForm = new SearchFormModel();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (_sportId != SportId)
            {

                TopMarketTemplates = (await BackOfficeService.GetTopMarkets(SportId)).Result;

                if (TopMarketTemplates != null && TopMarketTemplates.Any())
                    MarketTemplateId = TopMarketTemplates.FirstOrDefault().MainMarketTemplateId;

                await PrepareMonitoringData();
            }
            else if (_sportId != SportId || _segmentId != SegmentId || _leagueId != LeagueId)
            {
                await PrepareMonitoringData();
            }
        }

        public async void OnMarketChangedEvent(ChangeEventArgs e)
        {
            MarketTemplateId = e.Value.ToString();

            await OnInitCallback.InvokeAsync(new MonitoringCallbackModel
            {
                SportId = SportId,
                SelectedMarketTemplateId = MarketTemplateId,
                SearchText = SearchForm.SearchText
            });
        }

        public async Task OnInputSearchEvent()
        {
            if (!string.IsNullOrEmpty(SearchForm.SearchText) && SearchForm.SearchText.Length == 1)
                return;

            await OnInitCallback.InvokeAsync(new MonitoringCallbackModel
            {
                SportId = SportId,
                SearchText = SearchForm.SearchText,
                SelectedMarketTemplateId = MarketTemplateId 
            });
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private async Task PrepareMonitoringData()
        {
            _sportId = SportId;
            _segmentId = SegmentId;
            _leagueId = LeagueId;

            await OnInitCallback.InvokeAsync(new MonitoringCallbackModel
            {
                SportId = SportId,
                SearchText = string.Empty,
                SelectedMarketTemplateId = MarketTemplateId
            });
        }

        #endregion
    }
}
