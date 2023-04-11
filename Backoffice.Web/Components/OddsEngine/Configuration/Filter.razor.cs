using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Response.MarketTemplateGroupResponse;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class Filter : IDisposable
    {
        #region Properties        
        [Inject] private ITradingService TradingService { get; set; }        

        [Parameter] public ConfigFilterModel ConfigFilterModel { get; set; } = new ConfigFilterModel();
        [Parameter] public EventCallback<ConfigFilterModel> ConfigFilterModelChanged { get; set; }
        [Parameter] public EventCallback IsMarketEditingChanged { get; set; }
        [Parameter] public FixtureType FixtureType { get; set; }
        [Parameter] public string Region { get; set; } = string.Empty;
        #endregion

        #region Fields
        private bool _isPreloader { get; set; }
        private MarketTemplateGroupResponse _marketTemplateGroupResponse { get; set; }
        private MarketLookupResponse _marketLookup { get; set; }
        #endregion

        #region Methods        
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await Initialize();
            }
        }

        public async Task Initialize()
        {
            if (!string.IsNullOrEmpty(Region))
                TradingService.Region = Region;

            _isPreloader = true;
            var sports = await GetSports();
            var sportId = sports.Any() ? sports.FirstOrDefault().Value : 0;
            var headerTitle = sports.Any() ? sports.FirstOrDefault(c => c.Value == sportId).Name : "";

            var segments = await GetSegments(sportId);
            var segmentId = segments.Any() ? segments.FirstOrDefault().Value : 0;

            var leagues = await GetLeagues(sportId, segmentId);
            var leagueId = leagues.Any() ? leagues.FirstOrDefault().Value : 0;

            var scopes = GetScopes();
            var scopeId = int.Parse(scopes.FirstOrDefault().Value);

            var configStatuses = GetConfigStatus();
            var configStatus = int.Parse(configStatuses.FirstOrDefault().Value);

            var clients = await GetClients();
            var clientCode = string.Empty;
            var clientId = string.Empty;

            if (clients != null && clients.Any())
            {
                clientCode = clients.FirstOrDefault().ClientCode;
                clientId = clients.FirstOrDefault().ClientId.ToString();
            }

            var clientConfig = (await TradingService.GetClientConfig(new ClientConfigRequest
            {
                ClientCode = clientCode,
                SportId = sportId,
                SegmentId = segmentId,
                LeagueId = leagueId,
                FixtureTypeId = (int)FixtureType,
                IsActive = configStatus
            })).Result;

            var marketTemplateGroups = await GetMarketTemplateGroup(scopeId, sportId);
            var marketTemplateGroup = marketTemplateGroups.Count > 0 && marketTemplateGroups.Any(g => g.MarketTemplateGroup.MarketTemplateGroupId == 2) ?
                marketTemplateGroups.FirstOrDefault(g => g.MarketTemplateGroup.MarketTemplateGroupId == 2).MarketTemplateGroup :
                new MarketTemplateGroup() { MarketTemplateGroupId = 0, MarketTemplateGroupName = string.Empty };

            ConfigFilterModel = new ConfigFilterModel()
            {
                Sports = sports,
                SportId = sportId,
                HeaderTitle = headerTitle,
                Segments = segments,
                SegmentId = segmentId,
                Leagues = leagues,
                LeagueId = leagueId,
                Scopes = scopes,
                Scope = scopeId,
                ConfigStatuses = configStatuses,
                FilterConfigStatus = configStatus,
                Clients = clients,
                ClientCode = clientCode,
                ClientId = clientId,
                MarketTemplateGroups = marketTemplateGroups,
                MarketTemplates = marketTemplateGroups.Count > 0 ? await GetMarketTemplate(marketTemplateGroups) : new List<MarketLookupNavigationModel>(),
                MarketTemplateGroupId = marketTemplateGroup.MarketTemplateGroupId,
                MarketTemplateGroupName = marketTemplateGroup.MarketTemplateGroupName,
                ClientConfig = clientConfig
            };

            await ConfigFilterModelChanged.InvokeAsync(ConfigFilterModel);
            _isPreloader = false;
            StateHasChanged();   
        }

        public async Task OnSportChangedEvent(ChangeEventArgs e)
        {
            _isPreloader = true;
            ConfigFilterModel.MarketTemplates = null;
            await IsMarketEditingChanged.InvokeAsync();
            await ConfigFilterModelChanged.InvokeAsync(ConfigFilterModel);

            await Task.Delay(100);

            ConfigFilterModel.SportId = int.Parse(e.Value.ToString()); //((SelectListItemModel<int>)item).Value;
            ConfigFilterModel.HeaderTitle = ConfigFilterModel.Sports.FirstOrDefault(c => c.Value == ConfigFilterModel.SportId).Name;

            ConfigFilterModel.Segments = await GetSegments(ConfigFilterModel.SportId);
            ConfigFilterModel.SegmentId = ConfigFilterModel.Segments.Any() ? ConfigFilterModel.Segments.FirstOrDefault().Value : 0;

            ConfigFilterModel.Leagues = await GetLeagues(ConfigFilterModel.SportId, ConfigFilterModel.SegmentId);
            ConfigFilterModel.LeagueId = ConfigFilterModel.Leagues.Any() ? ConfigFilterModel.Leagues.FirstOrDefault().Value : 0;

            ConfigFilterModel.SearchText = string.Empty;
            ConfigFilterModel.MarketTemplateGroups = await GetMarketTemplateGroup(ConfigFilterModel.Scope, ConfigFilterModel.SportId);
            ConfigFilterModel.MarketTemplates = await GetMarketTemplate(ConfigFilterModel.MarketTemplateGroups);

            ConfigFilterModel.ClientConfig = (await TradingService.GetClientConfig(new ClientConfigRequest
            {
                ClientCode = ConfigFilterModel.ClientCode,
                SportId = ConfigFilterModel.SportId,
                SegmentId = ConfigFilterModel.SegmentId,
                LeagueId = ConfigFilterModel.LeagueId,
                FixtureTypeId = (int)FixtureType,
                IsActive = -99
            })).Result;

            InitMarketTemplateField();
            _isPreloader = false;

            await ConfigFilterModelChanged.InvokeAsync(ConfigFilterModel);
        }
        public async Task OnSegmentChangedEvent(ChangeEventArgs e)
        {
            _isPreloader = true;
            ConfigFilterModel.MarketTemplates = null;
            await IsMarketEditingChanged.InvokeAsync();
            await ConfigFilterModelChanged.InvokeAsync(ConfigFilterModel);

            await Task.Delay(100);

            ConfigFilterModel.MarketTemplates = await GetMarketTemplate(ConfigFilterModel.MarketTemplateGroups);

            ConfigFilterModel.SegmentId = int.Parse(e.Value.ToString());  //((SelectListItemModel<int>)item).Value;

            ConfigFilterModel.Leagues = await GetLeagues(ConfigFilterModel.SportId, ConfigFilterModel.SegmentId);
            ConfigFilterModel.LeagueId = ConfigFilterModel.Leagues.Any() ? ConfigFilterModel.Leagues.FirstOrDefault().Value : 0;

            ConfigFilterModel.ClientConfig = (await TradingService.GetClientConfig(new ClientConfigRequest
            {
                ClientCode = ConfigFilterModel.ClientCode,
                SportId = ConfigFilterModel.SportId,
                SegmentId = ConfigFilterModel.SegmentId,
                LeagueId = ConfigFilterModel.LeagueId,
                FixtureTypeId = (int)FixtureType,
                IsActive = -99
            })).Result;

            ConfigFilterModel.SearchText = string.Empty;

            InitMarketTemplateField();
            _isPreloader = false;

            await ConfigFilterModelChanged.InvokeAsync(ConfigFilterModel);
        }
        public async Task OnLeagueChangedEvent(ChangeEventArgs e)
        {
            _isPreloader = true;
            ConfigFilterModel.MarketTemplates = null;
            await IsMarketEditingChanged.InvokeAsync();
            await ConfigFilterModelChanged.InvokeAsync(ConfigFilterModel);

            await Task.Delay(100);

            ConfigFilterModel.MarketTemplates = await GetMarketTemplate(ConfigFilterModel.MarketTemplateGroups);

            ConfigFilterModel.LeagueId = int.Parse(e.Value.ToString());

            ConfigFilterModel.ClientConfig = (await TradingService.GetClientConfig(new ClientConfigRequest
            {
                ClientCode = ConfigFilterModel.ClientCode,
                SportId = ConfigFilterModel.SportId,
                SegmentId = ConfigFilterModel.SegmentId,
                LeagueId = ConfigFilterModel.LeagueId,
                FixtureTypeId = (int)FixtureType,
                IsActive = -99
            })).Result;

            ConfigFilterModel.SearchText = string.Empty;

            InitMarketTemplateField();
            _isPreloader = false;

            await ConfigFilterModelChanged.InvokeAsync(ConfigFilterModel);
        }
        public async Task OnScopeChangedEvent(ChangeEventArgs e)
        {
            _isPreloader = true;
            ConfigFilterModel.MarketTemplates = null;
            await IsMarketEditingChanged.InvokeAsync();
            await ConfigFilterModelChanged.InvokeAsync(ConfigFilterModel);

            await Task.Delay(100);

            ConfigFilterModel.Scope = int.Parse(e.Value.ToString());  //int.Parse(((SelectListItemModel<string>)item).Value.ToString());
            ConfigFilterModel.MarketTemplateGroups = await GetMarketTemplateGroup(ConfigFilterModel.Scope, ConfigFilterModel.SportId);
            ConfigFilterModel.MarketTemplates = await GetMarketTemplate(ConfigFilterModel.MarketTemplateGroups);

            ConfigFilterModel.SearchText = string.Empty;

            InitMarketTemplateField();
            _isPreloader = false;

            await ConfigFilterModelChanged.InvokeAsync(ConfigFilterModel);
        }
        public async Task OnClientChangedEvent(ChangeEventArgs e)
        {
            _isPreloader = true;
            ConfigFilterModel.MarketTemplates = null;
            await IsMarketEditingChanged.InvokeAsync();
            await ConfigFilterModelChanged.InvokeAsync(ConfigFilterModel);

            await Task.Delay(100);

            ConfigFilterModel.MarketTemplates = await GetMarketTemplate(ConfigFilterModel.MarketTemplateGroups);

            ConfigFilterModel.ClientCode = e.Value.ToString(); //((ClientInformationModel)item).ClientCode;
            ConfigFilterModel.ClientId = ConfigFilterModel.Clients.Where(c => c.ClientCode == ConfigFilterModel.ClientCode).FirstOrDefault().ClientId.ToString();
            ConfigFilterModel.ClientConfig = (await TradingService.GetClientConfig(new ClientConfigRequest
            {
                ClientCode = ConfigFilterModel.ClientCode,
                SportId = ConfigFilterModel.SportId,
                SegmentId = ConfigFilterModel.SegmentId,
                LeagueId = ConfigFilterModel.LeagueId,
                FixtureTypeId = (int)FixtureType,
                IsActive = -99
            })).Result;

            ConfigFilterModel.SearchText = string.Empty;

            InitMarketTemplateField();
            _isPreloader = false;

            await ConfigFilterModelChanged.InvokeAsync(ConfigFilterModel);
        }
        public async Task OnConfigChangedEvent(ChangeEventArgs e)
        {
            _isPreloader = true;
            ConfigFilterModel.FilterConfigStatus = int.Parse(e.Value.ToString());
            await IsMarketEditingChanged.InvokeAsync();

            ConfigFilterModel.SearchText = string.Empty;

            InitMarketTemplateField();
            _isPreloader = false;
        }
        public async Task OnMarketChangedEvent(ChangeEventArgs e)
        {
            _isPreloader = true;
            ConfigFilterModel.MarketTemplates = null;
            await IsMarketEditingChanged.InvokeAsync();
            await ConfigFilterModelChanged.InvokeAsync(ConfigFilterModel);

            await Task.Delay(100);

            ConfigFilterModel.MarketTemplateGroupId = int.Parse(e.Value.ToString());
            ConfigFilterModel.MarketTemplateGroupName = ConfigFilterModel.MarketTemplateGroups.FirstOrDefault(c => c.MarketTemplateGroup.MarketTemplateGroupId == ConfigFilterModel.MarketTemplateGroupId).MarketTemplateGroup.MarketTemplateGroupName;

            ConfigFilterModel.MarketTemplates = await GetMarketTemplate(ConfigFilterModel.MarketTemplateGroups);

            ConfigFilterModel.SearchText = string.Empty;

            InitMarketTemplateField();
            _isPreloader = false;

            await ConfigFilterModelChanged.InvokeAsync(ConfigFilterModel);
        }
        public void Dispose()
        {
            _marketTemplateGroupResponse = null;
            _marketLookup = null;

            TradingService.Dispose();

            GC.SuppressFinalize(this);
        }
        #endregion

        #region Utilities
        private async Task<List<SelectListItemModel<int>>> GetSports()
        {
            var result = (await TradingService.GetSportLookup()).Result;

            if (result == null)
                return new List<SelectListItemModel<int>>();

            var sports = result.SportLookup?.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SportId,
                Name = s.SportName
            }).ToList();

            return sports;
        }

        private async Task<List<SelectListItemModel<int>>> GetSegments(int sportId)
        {
            var result = (await TradingService.GetSegmentLookup(sportId)).Result;

            var segments = result.SegmentLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SegmentId,
                Name = s.SegmentName
            }).ToList();

            return segments;
        }

        private async Task<List<SelectListItemModel<int>>> GetLeagues(int sportId, int segmentId)
        {
            var result = (await TradingService.GetLeagueLookup(sportId, segmentId)).Result;

            var leagues = result.LeagueLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.LeagueId,
                Name = s.LeagueName
            }).ToList();

            return leagues;
        }

        private List<SelectListItemModel<string>> GetScopes()
        {
            var scopes = new List<SelectListItemModel<string>>()
            {
                new SelectListItemModel<string>() { Value = "99", Name = "All Scopes" },
                new SelectListItemModel<string>() { Value = ((int)Scope.Prematch).ToString(), Name = Scope.Prematch.ToString() },
                new SelectListItemModel<string>() { Value = ((int)Scope.Live).ToString(), Name = Scope.Live.ToString() },
            };

            return scopes;
        }

        private List<SelectListItemModel<string>> GetConfigStatus()
        {
            var scopes = new List<SelectListItemModel<string>>()
            {
                new SelectListItemModel<string>() { Value = "-99", Name = "All Config Status" },
                new SelectListItemModel<string>() { Value = ((int)MarketStatus.Active).ToString(), Name = "Active Market" },
                new SelectListItemModel<string>() { Value = ((int)MarketStatus.Inactive).ToString(), Name = "Inactive Market" },
            };

            return scopes;
        }

        private async Task<List<ClientInformationModel>> GetClients()
        {
            var result = (await TradingService.GetClientList()).Result;

            var clients = result.Select(a => new ClientInformationModel
            {
                ClientCode = a.ClientCode,
                ClientId = a.ClientId,
                ClientName = a.ClientName
            }).ToList();

            return clients;
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
            _marketLookup = (await TradingService.GetMarketLookup(new MarketLookupRequest { FixtureTypeId = (int)FixtureType, SportId = sportId })).Result;

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

        private void InitMarketTemplateField()
        {
            if (ConfigFilterModel.MarketTemplates != null)
            {
                foreach (var market in ConfigFilterModel.MarketTemplates)
                {
                    market.LiveSettings = null;
                    market.PrematchSettings = null;
                    market.NewLiveSettings = null;
                    market.NewPrematchSettings = null;
                    market.NewMarketLookup = null;
                }
            }

        }
        #endregion
    }
}
