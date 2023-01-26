using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Response.FastOddsFixturesResponse;

namespace Backoffice.Web.Components.MonitoringDetails
{
    public partial class MarketGroupFilter : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }

        //[Parameter] public EventCallback<MarketFilterCallbackModel> OnInitMarketTemplates { get; set; }
        [Parameter] public EventCallback<string> OnInputSearchCallback { get; set; }
        [Parameter] public EventCallback<MarketGroupNavigationModel> OnMarketTemplateGroupCallback { get; set; }
        [Parameter] public MarketFixtureResponse.FixtureModel Fixture { get; set; }
        [Parameter] public int SportId { get; set; }

        public SearchFormModel SearchForm { get; set; }
        public List<MarketGroupNavigationModel> MarketTemplateGroups { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SearchForm = new SearchFormModel();
            MarketTemplateGroups = new List<MarketGroupNavigationModel>();

            // 99 = all fixture types
            var marketTemplateGroupResponse = (await BackOfficeService.GetMarketTemplateGroups(99));

            if (marketTemplateGroupResponse.StatusCode == StatusCode.Success)
            {
                var marketTemplateGroup = marketTemplateGroupResponse.Result.Sports.FirstOrDefault(c => c.SportId == SportId);

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

                await OnMarketTemplateGroupCallback.InvokeAsync(MarketTemplateGroups.FirstOrDefault(c => c.IsSelected));
            }
        }

        //public async Task OnInitMarketTemplateGroup(MarketFixtureResponse.FixtureModel fixture)
        //{
        //    #region Market Template Group Nagivation

        //    MarketTemplateGroups = new List<MarketGroupNavigationModel>();

        //    // 99 = all fixture types
        //    var marketTemplateGroupResponse = (await BackOfficeService.GetMarketTemplateGroups(99)).Result;
        //    var sportMarketTemplateGroup = marketTemplateGroupResponse.Sports.FirstOrDefault(s => s.SportId == fixture.SportId);

        //    var sportMarketTemplateGroupLive = new List<MarketTemplateGroup>();
        //    var sportMarketTemplateGroupPrematch = new List<MarketTemplateGroup>();

        //    if (sportMarketTemplateGroup != null)
        //    {
        //        sportMarketTemplateGroupLive = sportMarketTemplateGroup?.Live;
        //        sportMarketTemplateGroupPrematch = sportMarketTemplateGroup?.Prematch;
        //    }

        //    List<MarketTemplateGroup> fullMarketTemplateGroup = sportMarketTemplateGroupLive;

        //    if (sportMarketTemplateGroupPrematch.Count > 0)
        //    {
        //        fullMarketTemplateGroup.AddRange(sportMarketTemplateGroupPrematch);
        //    }

        //    var marketTemplateGroupNames = fullMarketTemplateGroup.Select(s => s.MarketTemplateGroupName).Distinct().ToList();
        //    var marketTemplateGroupId = 1;

        //    foreach (var marketTemplateGroupName in marketTemplateGroupNames)
        //    {
        //        var marketTemplates = new List<string>();
        //        var rawMarketTemplate = fullMarketTemplateGroup.Where(c => c.MarketTemplateGroupName == marketTemplateGroupName).Select(s => s.MarketTemplateIds).ToList();

        //        foreach (var lst in rawMarketTemplate)
        //        {
        //            marketTemplates.AddRange(lst);
        //        }

        //        foreach (var markettemplate in fixture.Markets)
        //        {
        //            if (marketTemplates.Distinct().Contains(markettemplate.MarketTemplateId))
        //            {
        //                MarketTemplateGroups.Add(new MarketGroupNavigationModel
        //                {
        //                    IsSelected = MarketTemplateGroups.Count == 1 ? true : false,
        //                    MarketTemplateGroup = new MarketTemplateGroup
        //                    {
        //                        MarketTemplateGroupId = marketTemplateGroupId,
        //                        MarketTemplateGroupName = fullMarketTemplateGroup.FirstOrDefault(s => s.MarketTemplateGroupName == marketTemplateGroupName)?.MarketTemplateGroupName,
        //                        SortOrder = fullMarketTemplateGroup.FirstOrDefault(s => s.MarketTemplateGroupName == marketTemplateGroupName).SortOrder,
        //                        MarketTemplateIds = marketTemplates.Distinct().ToList()
        //                    }
        //                });

        //                break;
        //            }
        //        }

        //        marketTemplateGroupId++;
        //    }

        //    #endregion

        //    await InvokeAsync(StateHasChanged);

        //    await OnInitMarketTemplates.InvokeAsync(new MarketFilterCallbackModel
        //    {
        //        SearchForm = SearchForm,
        //        MarketTemplateGroups = MarketTemplateGroups
        //    });
        //}

        public async Task OnMarketTemplateGroupClickEvent(MarketGroupNavigationModel model)
        {
            if (model.IsSelected)
                return;

            MarketTemplateGroups.ForEach(s =>
            {
                s.IsSelected = false;
            });

            model.IsSelected = true;

            await OnMarketTemplateGroupCallback.InvokeAsync(model);
        }

        public async Task OnInputSearchEvent()
        {
            await OnInputSearchCallback.InvokeAsync(SearchForm.SearchText);
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
