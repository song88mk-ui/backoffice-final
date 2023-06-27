using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using static System.Formats.Asn1.AsnWriter;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ConfigEditTemplate : IDisposable
    {
        #region Properties
        
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }

        [Parameter] public string Region { get; set; } = "hk";
        [Parameter] public long ConfigTemplateId { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public List<MarketLookupNavigationModel> MarketLookupNavigation { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Leagues { get; set; }

        public ConfigTemplateModel ConfigTemplate { get; set; }
        public ConfigTemplateMarginModel ConfigTemplateMargin { get; set; }
        public EditConfigTemplateFilterModel EditConfigTemplateFilterModel { get; set; }
        public int SelectedClientCount { get; set; }
        public string SelectedClientName { get; set; } = "";
        public int SelectedLeagueCount { get; set; }
        public string SelectedLeagueName { get; set; } = "";
        public string SportIcon { get; set; } = "";
        public string SportName { get; set; }
        public int ScopeId { get; set; }

        #endregion

        #region Fields
        private MarketTemplateGroupResponse _marketTemplateGroupResponse { get; set; }
        private MarketLookupResponse _marketLookup { get; set; }
        #endregion


        #region Methods

        protected override async Task OnInitializedAsync()
        {            
            ConfigTemplate = new ConfigTemplateModel();

            Clients = new List<SelectListItemWithCheckboxModel<string>>();
            Leagues = new List<SelectListItemWithCheckboxModel<string>>();
            MarketLookupNavigation = new List<MarketLookupNavigationModel>();                        

            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = $"hk/odds-engine/config/my-templates",
                    IsCurrentPage = false
                }                
            };

            await LoadConfigTemplate();
            await LoadConfigTemplateMargin();
            await LoadMarkets();
        }

        public async Task InitConfigEditTemplate(EditConfigTemplateFilterModel model)
        {
            EditConfigTemplateFilterModel = model;

            //SelectedContents = await GetContents();
            //SelectedContents.ForEach(market => GetNewSettings(market));

            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            
        }

        #endregion

        #region Utilities      

        private async Task LoadConfigTemplate()
        {
            var response = await BackOfficeService.GetClientConfigTemplateById(ConfigTemplateId);

            if (response.StatusCode == StatusCode.Success)
            {
                ConfigTemplate = response.Result;
                
                SportName = (await TradingService.GetSportLookup()).Result.SportLookup.FirstOrDefault(e => e.SportId == response.Result.SportId).SportName;
                ScopeId = response.Result.FixtureTypeId;

                SportIcon = SportName.Contains(" ") ? SportName.Replace(" ", "-").ToLower() : SportName.ToLower();

                Breadcrumbs.Add(
                    new BreadcrumbModel
                    {
                        Name = $"{ConfigTemplate.Name}",
                        Url = $"hk/odds-engine/config/edit/{ConfigTemplateId}",
                        IsCurrentPage = true
                    });
            }
        }

        private async Task LoadConfigTemplateMargin()
        {
            var response = await BackOfficeService.GetConfigTemplateMargin(UserModel.Id, ConfigTemplateId);

            if (response.StatusCode == StatusCode.Success)
                ConfigTemplateMargin = response.Result;
        }


        private async Task LoadMarkets()
        {
            var marketTemplateGroups = await GetMarketTemplateGroup(ScopeId, ConfigTemplate.SportId);
            var marketTemplateGroup = marketTemplateGroups.Count > 0 && marketTemplateGroups.Any(g => g.MarketTemplateGroup.MarketTemplateGroupId == 2) ?
                marketTemplateGroups.FirstOrDefault(g => g.MarketTemplateGroup.MarketTemplateGroupId == 2).MarketTemplateGroup :
                new MarketTemplateGroup() { MarketTemplateGroupId = 0, MarketTemplateGroupName = string.Empty };

            if (marketTemplateGroups.Count > 0)
            {
                MarketLookupNavigation =  await GetMarketTemplate(marketTemplateGroups);
            }
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
            _marketLookup = (await TradingService.GetMarketLookup(new MarketLookupRequest { FixtureTypeId = (int)ScopeId, SportId = sportId })).Result;

            if (_marketLookup != null)
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

                        foreach (var markettemplate in _marketLookup.MarketLookup)
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
                            MarketTemplateIds = _marketLookup.MarketLookup.Select(s => s.MarketTemplateId).Distinct().ToList()
                        }
                    });
                }

            }

            return MarketTemplateGroups;
        }

        private async Task<List<MarketLookupNavigationModel>> GetMarketTemplate(List<MarketGroupNavigationModel> MarketTemplateGroups)
        {
            var marketTemplates = new List<MarketLookupNavigationModel>();
            foreach (var MarketTemplateGroup in MarketTemplateGroups)
            {
                var marketTemplateGroupId = MarketTemplateGroup.MarketTemplateGroup.MarketTemplateGroupId;
                foreach (var marketLookup in _marketLookup.MarketLookup)
                {
                    if (MarketTemplateGroup.MarketTemplateGroup.MarketTemplateIds.Contains(marketLookup.MarketTemplateId))
                    {
                        marketTemplates.Add(new MarketLookupNavigationModel()
                        {
                            IsSelected = !marketTemplates.Where(m => m.MarketTemplateGroupId == marketTemplateGroupId && m.IsSelected).Any(),
                            MarketTemplateGroupId = marketTemplateGroupId,
                            MarketLookup = marketLookup,
                            LiveSettings = new LiveSettingsModel(),
                            PrematchSettings = new PrematchSettingsModel()
                        });
                    }
                }
            }
            return marketTemplates;
        }

        #endregion
    }
}
