using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Serilog;
using static Backoffice.Shared.Models.Response.ClientListResponse;

namespace Backoffice.Web.Components.OddsEngine.MonitoringDetails
{
    public partial class CloseMarketsModal : IDisposable
    {
        #region Properties

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        protected MarketFixtureResponse.FixtureModel Fixture { get; set; }
        protected List<MarketGroupNavigationModel> MarketTemplateGroups { get; set; }
        protected List<MarketFixtureResponse.MarketModel> Markets { get; set; }

        public bool IsPreload { get; set; }
        public List<string> SelectedMarketTemplateIds { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Fixture = ModalParameters.TryGet<MarketFixtureResponse.FixtureModel>("Fixture");
            MarketTemplateGroups = new List<MarketGroupNavigationModel>();
            Markets = new List<MarketFixtureResponse.MarketModel>();
            SelectedMarketTemplateIds = new List<string>();

            if (Fixture.Markets.Any())
            {
                foreach(var market in Fixture.Markets)
                {
                    
                    if(!Markets.Any(m => m.MarketTemplateId == market.MarketTemplateId))
                    {
                        var newMarket = new MarketFixtureResponse.MarketModel
                        {
                            MarketName = market.MarketName,
                            MarketTemplateId = market.MarketTemplateId,
                            MarketSpecifier = market.MarketSpecifier,
                            MarketStatusId = market.MarketStatusId
                        };

                        Markets.Add(newMarket);
                    }
                }
            }

            // 99 = all fixture types
            var marketTemplateGroupResponse = (await _backofficeService.GetMarketTemplateGroups(99));

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

        public async Task OnMarketGroupRadioClickEvent(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
                return;

            MarketTemplateGroups.ForEach((c) => { c.IsSelected = false; });

            var marketGroupTemplate = MarketTemplateGroups.FirstOrDefault(m => m.MarketTemplateGroup.MarketTemplateGroupId == Convert.ToInt32(groupId.Replace("radio", "")));
            if(marketGroupTemplate != null)
            {
                marketGroupTemplate.IsSelected = true;
                SelectedMarketTemplateIds = marketGroupTemplate.MarketTemplateGroup.MarketTemplateIds;
            }


            await _jsRuntime.InvokeVoidAsync("scrollIntoElement", $"#lbl-{groupId}");
        }

        public async Task OnMarketChecked(string marketTemplateId)
        {
            var market = Markets.FirstOrDefault(m => m.MarketTemplateId == marketTemplateId);
            if (market != null)
            {
                market.MarketStatusId = market.MarketStatusId == (int)MarketStatus.Active ? (int)MarketStatus.Inactive : (int)MarketStatus.Active;
            }
        }

        public async Task OnCancelClickEvent()
        {
            _modalService.Cancel();
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
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }

            IsPreload = false;
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
