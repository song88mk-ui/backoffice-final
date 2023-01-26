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
        [Parameter] public EventCallback<MarketFilterCallbackModel> OnInitMarketTemplates { get; set; }
        public SearchFormModel SearchForm { get; set; }
        public List<MarketGroupNavigationModel> MarketTemplateGroups { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SearchForm = new SearchFormModel();

        }

        public async Task OnInitMarketTemplateGroup(MarketFixtureResponse.FixtureModel fixture)
        {

            #region Market Template Group Nagivation
            MarketTemplateGroups = new List<MarketGroupNavigationModel>();

            var marketTemplateGroupResponse = (await BackOfficeService.GetMarketTemplateGroups(99)).Result;
            var sportMarketTemplateGroup = marketTemplateGroupResponse.Sports.FirstOrDefault(s => s.SportId == fixture.SportId);

            var sportMarketTemplateGroupLive = new List<MarketTemplateGroup>();
            var sportMarketTemplateGroupPrematch = new List<MarketTemplateGroup>();

            if (sportMarketTemplateGroup != null)
            {
                sportMarketTemplateGroupLive = sportMarketTemplateGroup?.Live;
                sportMarketTemplateGroupPrematch = sportMarketTemplateGroup?.Prematch;
            }

            List<MarketTemplateGroup> fullMarketTemplateGroup = sportMarketTemplateGroupLive;

            if (sportMarketTemplateGroupPrematch.Count > 0)
            {
                fullMarketTemplateGroup.AddRange(sportMarketTemplateGroupPrematch);
            }

            var marketTemplateGroupNames = fullMarketTemplateGroup.Select(s => s.MarketTemplateGroupName).Distinct().ToList();
            var marketTemplateGroupId = 1;

            foreach (var marketTemplateGroupName in marketTemplateGroupNames)
            {
                var marketTemplates = new List<string>();
                var rawMarketTemplate = fullMarketTemplateGroup.Where(c => c.MarketTemplateGroupName == marketTemplateGroupName).Select(s => s.MarketTemplateIds).ToList();

                foreach (var lst in rawMarketTemplate)
                {
                    marketTemplates.AddRange(lst);
                }

                foreach (var markettemplate in fixture.Markets)
                {
                    if (marketTemplates.Distinct().Contains(markettemplate.MarketTemplateId))
                    {
                        MarketTemplateGroups.Add(new MarketGroupNavigationModel
                        {
                            IsSelected = MarketTemplateGroups.Count == 1 ? true : false,
                            MarketTemplateGroup = new MarketTemplateGroup
                            {
                                MarketTemplateGroupId = marketTemplateGroupId,
                                MarketTemplateGroupName = fullMarketTemplateGroup.FirstOrDefault(s => s.MarketTemplateGroupName == marketTemplateGroupName)?.MarketTemplateGroupName,
                                SortOrder = fullMarketTemplateGroup.FirstOrDefault(s => s.MarketTemplateGroupName == marketTemplateGroupName).SortOrder,
                                MarketTemplateIds = marketTemplates.Distinct().ToList()
                            }
                        });

                        break;
                    }
                }

                marketTemplateGroupId++;
            }
            
            #endregion

            await InvokeAsync(StateHasChanged);

            await OnInitMarketTemplates.InvokeAsync(new MarketFilterCallbackModel
            {
                SearchForm = SearchForm,
                MarketTemplateGroups = MarketTemplateGroups
            });
        }

        public async Task OnMarketTemplateGroupClickEvent(int marketTemplateGroupId)
        {
            var isSelected = !MarketTemplateGroups.FirstOrDefault(c => c.MarketTemplateGroup.MarketTemplateGroupId == marketTemplateGroupId).IsSelected;

            if (isSelected)
            {
                foreach (var marketTemplateGroup in MarketTemplateGroups)
                {
                    if (marketTemplateGroup.MarketTemplateGroup.MarketTemplateGroupId != marketTemplateGroupId)
                    {
                        marketTemplateGroup.IsSelected = !isSelected;
                    }
                    else
                    {
                        marketTemplateGroup.IsSelected = isSelected;
                    }
                }
            }

            await OnInitMarketTemplates.InvokeAsync(new MarketFilterCallbackModel
            {
                SearchForm = SearchForm,
                MarketTemplateGroups = MarketTemplateGroups
            });
        }

        public async Task OnInputSearchEvent()
        {
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
