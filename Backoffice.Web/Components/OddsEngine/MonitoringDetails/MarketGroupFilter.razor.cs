using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.MonitoringDetails
{
    public partial class MarketGroupFilter : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }

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
            if (!string.IsNullOrEmpty(SearchForm.SearchText) && SearchForm.SearchText.Length == 1)
                return;

            await OnInputSearchCallback.InvokeAsync(SearchForm.SearchText);
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
