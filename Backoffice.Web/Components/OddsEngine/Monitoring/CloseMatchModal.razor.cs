using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Drawing;
using System.Text.Json;
using static Backoffice.Shared.Models.Response.FastOddsFixturesResponse;

namespace Backoffice.Web.Components.OddsEngine.Monitoring
{
    public partial class CloseMatchModal : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        protected FixtureMarketModel Fixture { get; set; }
        protected bool IsPreload { get; set; }
        #endregion        

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Fixture = ModalParameters.TryGet<FixtureMarketModel>("Market");
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnConfirmClickEvent()
        {
            IsPreload = true;

            var marketFixtureResponse = await BackOfficeService.GetMarketFixtureDetails(Fixture.ProviderFixtureId);

            if (marketFixtureResponse.StatusCode == StatusCode.Success)
            {
                var newFixture = marketFixtureResponse.Result.Fixture;

                var markets = new List<DeactivateFixtureMarketRequest.MarketMessage>();

                if(newFixture.Markets.Any())
                {
                    foreach(var market in newFixture.Markets)
                    {
                        if (market.Selections.Count == 0)
                            continue;

                        markets.Add(new DeactivateFixtureMarketRequest.MarketMessage
                        {
                            MarketTemplateId = market.MarketTemplateId,
                            MarketName = market.MarketName,
                            MarketTemplateName = market.MarketName,
                            MarketSpecifier = market.MarketSpecifier,
                            Line = market.Line,
                            MarketStatusId = market.MarketStatusId == (int)MarketStatus.Active ? (int)MarketStatus.Inactive : market.MarketStatusId,
                            Selections = market.Selections.Select(s => new DeactivateFixtureMarketRequest.SelectionMessage
                            {
                                IsActive = false,
                                SelectionName = s.SelectionName,
                                SelectionTemplateId = s.SelectionTemplateId,
                                SelectionSpecifier = s.SelectionSpecifier
                            }).ToList()
                        });
                    }
                }

                var request = new DeactivateFixtureMarketRequest
                {
                    ProviderFixtureId = Fixture.ProviderFixtureId,
                    SportId = newFixture.SportId,
                    SportName = newFixture.SportName,
                    Raw = JsonSerializer.Serialize(newFixture),
                    Markets = markets
                };

                var result = await BackOfficeService.DeactivateFixtureMarkets(request);

                if(result.Result)
                {
                    await Modal.OnSaveClickCallback.InvokeAsync();

                    ModalService.Cancel();

                    IsPreload = false;

                }
            }

            IsPreload = false;
        }

        public void Dispose()
        {

        }

        #endregion        
    }
}
