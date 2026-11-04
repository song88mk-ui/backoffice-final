using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Serilog;
using SportMarketTemplateGroup = Backoffice.Shared.Models.Response.MarketTemplateGroupResponse.SportMarketTemplateGroup;

namespace Backoffice.Web.Components.OddsEngine.MonitoringDetails
{
    public partial class MarketGroupFilter : IDisposable
    {
        #region Properties
        [Parameter] public EventCallback<string> OnInputSearchCallback { get; set; }
        [Parameter] public EventCallback<MarketGroupNavigationModel> OnMarketTemplateGroupCallback { get; set; }
        [Parameter] public MarketFixtureResponse.FixtureModel Fixture { get; set; }
        [Parameter] public int SportId { get; set; }

        public SearchFormModel SearchForm { get; set; }
        public List<MarketGroupNavigationModel> MarketTemplateGroups { get; set; }
        public AlertModel LoadAlert { get; set; } = new();

        private const string AllMarketsGroupName = "All Markets";

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SearchForm = new SearchFormModel();
            MarketTemplateGroups = new List<MarketGroupNavigationModel>();

            // The groups of every sport come from the BO API's Sportsbook data (BO-154), not the Odds Engine Trading API.
            var marketTemplateGroupResponse = await _backofficeService.GetConfigMarketTemplateGroups();

            if (marketTemplateGroupResponse?.StatusCode == StatusCode.Success)
            {
                var marketTemplateGroup = marketTemplateGroupResponse.Result?.Sports.FirstOrDefault(c => c.SportId == SportId);

                var allMarketTemplateGroups = new List<MarketTemplateGroup>();

                if (marketTemplateGroup != null)
                {
                    allMarketTemplateGroups.AddRange(marketTemplateGroup.Live);
                    allMarketTemplateGroups.AddRange(marketTemplateGroup.Prematch);
                }

                // A sport with no market group rows gets one group holding every market of the fixture, as in the Config editor.
                if (!allMarketTemplateGroups.Any())
                {
                    allMarketTemplateGroups.Add(new MarketTemplateGroup
                    {
                        MarketTemplateGroupName = AllMarketsGroupName,
                        SortOrder = 1,
                        MarketTemplateIds = Fixture.Markets.Select(m => m.MarketTemplateId).Distinct().ToList()
                    });
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

                // The page reads the group it is given, so it must not get null when no group matches the fixture's markets.
                var selectedGroup = MarketTemplateGroups.FirstOrDefault(c => c.IsSelected);

                if (selectedGroup != null)
                    await OnMarketTemplateGroupCallback.InvokeAsync(selectedGroup);
            }
            else
            {
                Log.Warning("The Monitoring market group filter could not load the market groups: {Message}", marketTemplateGroupResponse?.Message);

                LoadAlert = new AlertModel
                {
                    Status = AlertStatus.Error,
                    Message = "The market groups could not be loaded. Please try again, or contact support if it keeps happening.",
                    IsAlwaysShown = true
                };
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
