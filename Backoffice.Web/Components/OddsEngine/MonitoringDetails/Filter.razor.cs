using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Xml.Linq;
using static Backoffice.Shared.Models.Response.FastOddsSportsResponse;

namespace Backoffice.Web.Components.OddsEngine.MonitoringDetails
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Parameter] public EventCallback<MonitoringDetailsCallbackModel> OnInitCallback { get; set; }
        [Parameter] public string ProviderFixtureId { get; set; }
        public List<MainMarketTemplateResponse> TopMarketTemplates { get; set; }
        public string MarketTemplateId { get; set; } = string.Empty;
        public MarketFixtureDetailResponse Fixture { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            try
            {
                TopMarketTemplates = new List<MainMarketTemplateResponse>();

                Fixture = await GetFixtureDetails(ProviderFixtureId);

                var fixture = Fixture.Fixture;
                if (fixture != null)
                {
                    TopMarketTemplates = (await BackOfficeService.GetTopMarkets(fixture.SportId)).Result;

                    if (TopMarketTemplates != null && TopMarketTemplates.Any())
                        MarketTemplateId = TopMarketTemplates.FirstOrDefault().MainMarketTemplateId;
                }

                await OnInitCallback.InvokeAsync(new MonitoringDetailsCallbackModel
                {
                    SelectedMarketTemplateId = MarketTemplateId,
                    Fixture = Fixture
                });

            }
            catch (Exception)
            {
            }
        }

        public async void OnMarketChangedEvent(ChangeEventArgs e)
        {
            MarketTemplateId = e.Value.ToString();

            await OnInitCallback.InvokeAsync(new MonitoringDetailsCallbackModel
            {
                SelectedMarketTemplateId = MarketTemplateId,
                Fixture = Fixture
            });
        }

        public void Dispose()
        {
        }

        #endregion


        #region Utilities
        private async Task<MarketFixtureDetailResponse> GetFixtureDetails(string providerFixtureId)
        {
            var fixtureDetails = (await BackOfficeService.GetMarketFixtureDetails(providerFixtureId)).Result;

            if (fixtureDetails.Fixture == null)
                return new MarketFixtureDetailResponse();


            return fixtureDetails ?? new MarketFixtureDetailResponse();
        }
        #endregion
    }
}
