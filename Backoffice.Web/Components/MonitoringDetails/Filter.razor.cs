using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;

namespace Backoffice.Web.Components.MonitoringDetails
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }

        [Parameter] public EventCallback<MonitoringDetailsModel> OnInitCallback { get; set; }
        [Parameter] public string ProviderFixtureId { get; set; }

        public List<SelectListItemModel<string>> MarketTemplates { get; set; }
        public MonitoringDetailsModel MonitoringDetailsModel { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            MonitoringDetailsModel = new MonitoringDetailsModel();
            MarketTemplates = new List<SelectListItemModel<string>>();

            var marketFixtureResponse = await BackOfficeService.GetMarketFixtureDetails(ProviderFixtureId);

            if (marketFixtureResponse.StatusCode == StatusCode.Success)
            {
                var fixture = marketFixtureResponse.Result.Fixture;

                MonitoringDetailsModel = new MonitoringDetailsModel
                {
                    SportId = fixture.SportId,
                    SportName = fixture.SportName,
                    SegmentName = fixture.SegmentName,
                    LeagueName = fixture.LeagueName,
                    Fixtures = GetFixtureDetails(fixture),
                    SelectedMarketTemplateId = string.Empty
                };

            }

            if (MonitoringDetailsModel.Fixtures.Any())
            {
                var mainMarketTemplateResponse = await BackOfficeService.GetTopMarkets(MonitoringDetailsModel.SportId);

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
                    MonitoringDetailsModel.SelectedMarketTemplateId = MarketTemplates.FirstOrDefault().Value;
                }
            }

            await OnInitCallback.InvokeAsync(MonitoringDetailsModel);
        }

        public async void OnMarketChangedEvent(ChangeEventArgs e)
        {
            MonitoringDetailsModel.SelectedMarketTemplateId = e.Value.ToString();

            await OnInitCallback.InvokeAsync(MonitoringDetailsModel);
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private List<FixtureMarketModel> GetFixtureDetails(MarketFixtureResponse.FixtureModel fixture)
        {
            var result = fixture.Markets.Where(c => c.MarketTemplateId == MonitoringDetailsModel.SelectedMarketTemplateId)
                    .Select(s => new FixtureMarketModel
                    {
                        FixtureName = fixture.FixtureName,
                        MarketId = s.MarketId,
                        ClientCode = s.ClientCode,
                        ClientCodeMargin = s.ClientCodeMargin,
                        MarketName = s.MarketName,
                        ProviderMarketId = s.ProviderMarketId,
                        ProviderFixtureId = s.ProviderFixtureId,
                        AwayName = fixture.AwayName,
                        BetStop = fixture.BetStop,
                        CurrentMinute = fixture.CurrentMinute,
                        CurrentPeriod = fixture.CurrentPeriod,
                        FixtureStartDate = fixture.FixtureStartDate,
                        FixtureStatusId = fixture.FixtureStatusId,
                        HomeName = fixture.HomeName,
                        IsBetStopGlobal = fixture.IsBetStopGlobal,
                        Score = fixture.Score,
                        Line = s.Line,
                        MarketSpecifier = s.MarketSpecifier,
                        MarketStatusId = s.MarketStatusId,
                        MarketStatusName = s.MarketStatusName,
                        MarketTemplateId = s.MarketTemplateId,
                        Selections = s.Selections.Select(se => new SelectionModel
                        {
                            ProviderSelectionId = se.ProviderSelectionId,
                            SelectionId = se.SelectionId,
                            SelectionTemplateId = se.SelectionTemplateId,
                            SelectionName = se.SelectionName,
                            SelectionSpecifier = se.SelectionSpecifier,
                            DecimalOdds = se.DecimalOdds,
                            IsActive = se.IsActive,
                            SortOrder = se.SortOrder,
                            CorrelationId = se.CorrelationId,
                            OldDecimalOdds = se.OldDecimalOdds,
                            ClientOdds = se.ClientOdds.Select(od => new ClientOddsModel
                            {
                                ClientCode = od.ClientCode,
                                Odds = od.Odds,
                                OldOdds = od.OldOdds,
                                SelectionTemplateId = od.SelectionTemplateId,
                                SelectionId = od.SelectionId
                            }).ToList()
                        }).ToList()
                    }).ToList();

            return result;
        }

        #endregion
    }
}
