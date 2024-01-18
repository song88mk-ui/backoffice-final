using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Data.Backoffice.Entities;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class EditConfigTemplateFilter : IDisposable
    {
        #region Properties
        [Inject] private IBackofficeService BackofficeService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [Parameter] public EventCallback<EditConfigTemplateFilterModel> EditConfigTemplateFilterCallback { get; set; }
        [Parameter] public long ConfigTemplateId { get; set; }

        public SearchFormModel SearchForm { get; set; }
        public EditConfigTemplateFilterModel EditConfigTemplateFilterModel { get; set; }


        #endregion

        #region Fields
        private MarketTemplateGroupResponse _marketTemplateGroupResponse { get; set; }
        
        #endregion


        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SearchForm = new SearchFormModel();
            EditConfigTemplateFilterModel = new EditConfigTemplateFilterModel();
            EditConfigTemplateFilterModel.MarketGroups = new List<SelectListItemWithCheckboxModel<string>>();
            EditConfigTemplateFilterModel.ConfigStatuses = new List<SelectListItemWithCheckboxModel<string>>();

            await LoadConfigTemplateById();
            await LoadMarkets();
            await LoadConfigStatuses();

            await EditConfigTemplateFilterCallback.InvokeAsync(EditConfigTemplateFilterModel);
        }

        public async Task OnMarketCheckboxClickEvent(Dictionary<string, string> items)
        {
            EditConfigTemplateFilterModel.SelectedMarkets = items.Any() ? items.Where(i => i.Key != "all" && i.Key != "1").Select(i => i.Key).ToList() : new List<string>();
            EditConfigTemplateFilterModel.SelectedMarketCount = items.Count;

            await EditConfigTemplateFilterCallback.InvokeAsync(EditConfigTemplateFilterModel);
        }

        public async Task OnConfigCheckboxClickEvent(Dictionary<string, string> items)
        {
            EditConfigTemplateFilterModel.SelectedConfigStatus = items.Any() ? items.Where(i => i.Key != "all").Select(i => i.Key).ToList() : new List<string>();
            EditConfigTemplateFilterModel.SelectedConfigStatusCount = items.Count;

            await EditConfigTemplateFilterCallback.InvokeAsync(EditConfigTemplateFilterModel);
        }

        public async Task OnInputSearchEvent()
        {
            if (!string.IsNullOrEmpty(SearchForm.SearchText) && SearchForm.SearchText.Length == 1)
                return;

            EditConfigTemplateFilterModel.SearchText = SearchForm.SearchText;

            await EditConfigTemplateFilterCallback.InvokeAsync(EditConfigTemplateFilterModel);
        }


        public void Dispose()
        {
            
        }

        #endregion

        #region Utilities
        private async Task LoadConfigTemplateById()
        {
            var response = await BackofficeService.GetConfigTemplateById(ConfigTemplateId);

            if (response.StatusCode == StatusCode.Success && response.Result != null)
            {
                EditConfigTemplateFilterModel.ConfigTemplate = response.Result;

                EditConfigTemplateFilterModel.SportName = (await TradingService.GetSportLookup()).Result.SportLookup.FirstOrDefault(e => e.SportId == response.Result.SportId).SportName;
                EditConfigTemplateFilterModel.SportId = response.Result.SportId;
                EditConfigTemplateFilterModel.ScopeId = response.Result.FixtureStatusId;
            }
        }

        private async Task LoadMarkets()
        {
            EditConfigTemplateFilterModel.MarketTemplateGroups = await GetMarketTemplateGroup(EditConfigTemplateFilterModel.ScopeId, EditConfigTemplateFilterModel.ConfigTemplate.SportId);

            if (EditConfigTemplateFilterModel.MarketTemplateGroups.Count > 0)
            {
                EditConfigTemplateFilterModel.MarketGroups = new List<SelectListItemWithCheckboxModel<string>>();
                EditConfigTemplateFilterModel.MarketGroups.AddRange(EditConfigTemplateFilterModel.MarketTemplateGroups.Select(s =>
                    new SelectListItemWithCheckboxModel<string> { IsChecked = true, Name = s.MarketTemplateGroup.MarketTemplateGroupName, Value = s.MarketTemplateGroup.MarketTemplateGroupId.ToString(), IsSelectAll = s.MarketTemplateGroup.MarketTemplateGroupId == 1 }));
            }

            var selectedMarkets = EditConfigTemplateFilterModel.MarketGroups.Where(c => c.Value != "1" &&  c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

            EditConfigTemplateFilterModel.SelectedMarketCount = selectedMarkets.Count;
            EditConfigTemplateFilterModel.SelectedMarkets = EditConfigTemplateFilterModel.SelectedMarketCount > 0 ? selectedMarkets : new List<string>();

        }

        private async Task LoadConfigStatuses()
        {
            EditConfigTemplateFilterModel.ConfigStatuses = new List<SelectListItemWithCheckboxModel<string>>()
            {
                new SelectListItemWithCheckboxModel<string>(){ IsChecked = true, Name = "Select All", Value = "all" },
                new SelectListItemWithCheckboxModel<string>(){ IsChecked = true, Name = "Active Markets", Value = "1" },
                new SelectListItemWithCheckboxModel<string>(){ IsChecked = true, Name = "Inactive Markets", Value = "2" },                
            };

            var selectedConfigs = EditConfigTemplateFilterModel.ConfigStatuses.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

            EditConfigTemplateFilterModel.SelectedConfigStatusCount = selectedConfigs.Count;
            EditConfigTemplateFilterModel.SelectedConfigStatus = EditConfigTemplateFilterModel.SelectedConfigStatusCount > 0 ? EditConfigTemplateFilterModel.ConfigStatuses.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList() : new List<string>();
        }

        private async Task<List<MarketGroupNavigationModel>> GetMarketTemplateGroup(int scope, int sportId)
        {
            if (_marketTemplateGroupResponse == null)
                _marketTemplateGroupResponse = (await TradingService.GetMarketTemplateGroups(scope)).Result;

            if (_marketTemplateGroupResponse == null)
            {
                return new List<MarketGroupNavigationModel>();
            }

            var sportMarketTemplateGroup = _marketTemplateGroupResponse.Sports.Where(s => s.SportId == sportId).FirstOrDefault();

            if (sportMarketTemplateGroup == null)
            {
                sportMarketTemplateGroup = new MarketTemplateGroupResponse.SportMarketTemplateGroup()
                {
                    SportId = sportId
                };
            }

            var sportMarketTemplateGroupLive = sportMarketTemplateGroup?.Live;
            var sportMarketTemplateGroupPreMatch = sportMarketTemplateGroup?.Prematch;

            var tempMarketTemplateGroup = sportMarketTemplateGroupLive;
            if (sportMarketTemplateGroupPreMatch != null && sportMarketTemplateGroupPreMatch.Count > 0)
                tempMarketTemplateGroup.AddRange(sportMarketTemplateGroupPreMatch);

            var marketTemplateGroupNames = tempMarketTemplateGroup.Select(s => s.MarketTemplateGroupName).Distinct().ToList();

            var MarketTemplateGroups = new List<MarketGroupNavigationModel>();
            EditConfigTemplateFilterModel.MarketLookup = (await TradingService.GetMarketLookup(new MarketLookupRequest { FixtureTypeId = (int)FixtureType.Match, SportId = sportId })).Result;

            if (EditConfigTemplateFilterModel.MarketLookup != null)
            {
                if (marketTemplateGroupNames.Any())
                {
                    var marketTemplateGroupId = 0;
                    foreach (var marketTemplateGroupName in marketTemplateGroupNames)
                    {
                        marketTemplateGroupId++;
                        var marketTemplates = new List<string>();
                        var tempMarketTemplate = tempMarketTemplateGroup.Where(s => s.MarketTemplateGroupName == marketTemplateGroupName).Select(s => s.MarketTemplateIds).ToList();

                        foreach (var lst in tempMarketTemplate)
                        {
                            marketTemplates.AddRange(lst);
                        }

                        foreach (var markettemplate in EditConfigTemplateFilterModel.MarketLookup.MarketLookup)
                        {
                            if (marketTemplates.Distinct().ToList().Contains(markettemplate.MarketTemplateId))
                            {
                                MarketTemplateGroups.Add(new MarketGroupNavigationModel
                                {
                                    IsSelected = MarketTemplateGroups.Count == 1 ? true : false,
                                    MarketTemplateGroup = new MarketTemplateGroup
                                    {
                                        MarketTemplateGroupId = marketTemplateGroupId,
                                        MarketTemplateGroupName = tempMarketTemplateGroup.Where(s => s.MarketTemplateGroupName == marketTemplateGroupName).FirstOrDefault()?.MarketTemplateGroupName,
                                        SortOrder = tempMarketTemplateGroup.Where(s => s.MarketTemplateGroupName == marketTemplateGroupName).FirstOrDefault().SortOrder,
                                        MarketTemplateIds = marketTemplates.Distinct().ToList()
                                    }
                                });

                                break;
                            }
                        }
                    }
                }
                else
                {
                    MarketTemplateGroups.Add(new MarketGroupNavigationModel
                    {
                        IsSelected = true,
                        MarketTemplateGroup = new MarketTemplateGroup
                        {
                            MarketTemplateGroupId = 1,
                            MarketTemplateGroupName = "All Markets",
                            SortOrder = 1,
                            MarketTemplateIds = EditConfigTemplateFilterModel.MarketLookup.MarketLookup.Select(s => s.MarketTemplateId).Distinct().ToList()
                        }
                    });
                }

            }

            return MarketTemplateGroups;
        }

        #endregion
    }
}
