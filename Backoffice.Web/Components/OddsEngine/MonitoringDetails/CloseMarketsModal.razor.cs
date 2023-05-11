using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Response.ClientListResponse;

namespace Backoffice.Web.Components.OddsEngine.MonitoringDetails
{
    public partial class CloseMarketsModal : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        protected MarketFixtureResponse.FixtureModel Fixture { get; set; }
        protected List<MarketGroupNavigationModel> MarketTemplateGroups { get; set; }

        public bool IsPreload { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Fixture = ModalParameters.TryGet<MarketFixtureResponse.FixtureModel>("Fixture");
            MarketTemplateGroups = new List<MarketGroupNavigationModel>();

            // 99 = all fixture types
            var marketTemplateGroupResponse = (await BackOfficeService.GetMarketTemplateGroups(99));

            if (marketTemplateGroupResponse.StatusCode == StatusCode.Success)
            {
                var marketTemplateGroup = marketTemplateGroupResponse.Result.Sports.FirstOrDefault(c => c.SportId == Fixture.SportId);

                var allMarketTemplateGroups = new List<MarketTemplateGroup>();

                if (marketTemplateGroup != null)
                {
                    allMarketTemplateGroups.AddRange(marketTemplateGroup.Live);
                    allMarketTemplateGroups.AddRange(marketTemplateGroup.Prematch);
                }

                var marketTemplateGroupNames = allMarketTemplateGroups.Select(s => s.MarketTemplateGroupName).Distinct().ToList();
                var marketTemplateGroupId = 1;

                foreach (var item in marketTemplateGroupNames)
                {
                    var marketTemplateIds = allMarketTemplateGroups.Where(c => c.MarketTemplateGroupName == item)
                        .SelectMany(s => s.MarketTemplateIds)
                        .ToList();

                    var markets = Fixture.Markets.Where(c => marketTemplateIds.Contains(c.MarketTemplateId)).ToList();

                    if (markets.Any())
                    {
                        var selectedMarketTemplateGroup = allMarketTemplateGroups.FirstOrDefault(c => c.MarketTemplateGroupName == item);

                        MarketTemplateGroups.Add(new MarketGroupNavigationModel
                        {
                            IsSelected = MarketTemplateGroups.Count == 0 ? true : false,
                            MarketTemplateGroup = new MarketTemplateGroup
                            {
                                MarketTemplateGroupId = marketTemplateGroupId,
                                MarketTemplateGroupName = selectedMarketTemplateGroup.MarketTemplateGroupName,
                                SortOrder = selectedMarketTemplateGroup.SortOrder,
                                MarketTemplateIds = marketTemplateIds.Distinct().ToList()
                            }
                        });
                    }

                    marketTemplateGroupId++;
                }

            }
        }

        public async Task OnMarketGroupRadioClickEvent()
        {
            //MarketTemplateGroups.ForEach((c) => { c.IsSelected = false; });

            var marketGroupTemplate = MarketTemplateGroups.FirstOrDefault(m => m.MarketTemplateGroup.MarketTemplateGroupId == 0);
            if(marketGroupTemplate != null)
                marketGroupTemplate.IsSelected = !marketGroupTemplate.IsSelected;
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }
        public async Task OnSaveClickEvent()
        {
            try
            {
                if (IsPreload)
                    return;
            }
            catch (Exception ex)
            {

            }

            IsPreload = false;
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
