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
        [Parameter] public string ConfigName { get; set; }
        [Parameter] public int SportId { get; set; }
        [Parameter] public int ScopeId { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public List<MarketLookupNavigationModel> MarketLookupNavigation { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Leagues { get; set; }

        public string PageTitle { get; set; }
        public int SelectedClientCount { get; set; }
        public string SelectedClientName { get; set; } = "";
        public int SelectedLeagueCount { get; set; }
        public string SelectedLeagueName { get; set; } = "";

        #endregion

        #region Fields
        private MarketTemplateGroupResponse _marketTemplateGroupResponse { get; set; }
        private MarketLookupResponse _marketLookup { get; set; }
        #endregion


        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = ConfigName;

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
                },
                new BreadcrumbModel
                {
                    Name = $"{PageTitle}",
                    Url = $"hk/odds-engine/config/edit/{SportId}/{ScopeId}/{PageTitle}",
                    IsCurrentPage = true
                }
            };

            await LoadClients();
            await LoadLeagues();
            await LoadMarkets();
        }

        public async Task OnClientCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedClientName = string.Empty;
            SelectedClientCount = items.Count;

            if (SelectedClientCount == 1)
            {
                SelectedClientName = items.FirstOrDefault().Value;
            }
        }

        public async Task OnLeagueCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedLeagueName = string.Empty;
            SelectedLeagueCount = items.Count;

            if (SelectedLeagueCount == 1)
            {
                SelectedLeagueName = items.FirstOrDefault().Value;
            }
        }

        public void Dispose()
        {
            
        }

        #endregion

        #region Utilities
        private async Task LoadClients()
        {
            var response = (await TradingService.GetClientList()).Result;

            if (response != null && response.Any())
            {
                if (UserModel.ClientCodes.Any())
                    response = response.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();

                Clients = response.Select(e =>
                    new SelectListItemWithCheckboxModel<string>()
                    {
                        IsChecked = true,
                        Name = e.ClientName,
                        Value = e.ClientCode
                    }).ToList();

                Clients.Insert(0, new SelectListItemWithCheckboxModel<string>
                {
                    Value = "all",
                    Name = "All Clients",
                    IsChecked = true
                });

                var selectedClients = Clients.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

                SelectedClientCount = selectedClients.Count;
                SelectedClientName = SelectedClientCount > 0 ? string.Empty : Clients.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
            }
        }

        private async Task LoadLeagues()
        {
            var response = (await TradingService.GetLeagueLookup(21, 0)).Result;

            if (response != null && response.LeagueLookup.Any())
            {
                Leagues = response.LeagueLookup.Select(e =>
                    new SelectListItemWithCheckboxModel<string>()
                    {
                        IsChecked = true,
                        Name = e.LeagueName,
                        Value = e.LeagueId.ToString()
                    }).ToList();

                Leagues.Insert(0, new SelectListItemWithCheckboxModel<string>
                {
                    Value = "all",
                    Name = "All Leagues",
                    IsChecked = true
                });

                var selectedLeagues = Leagues.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

                SelectedLeagueCount = selectedLeagues.Count;
                SelectedLeagueName = SelectedLeagueCount > 0 ? string.Empty : Leagues.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
            }
        }

        private async Task LoadMarkets()
        {
            var marketTemplateGroups = await GetMarketTemplateGroup(ScopeId, SportId);
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
