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

        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public List<MarketLookupNavigationModel> MarketLookupNavigation { get; set; }

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
        public int SportId { get; set; }
        public bool IsPreloader { get; set; } = true;
        #endregion

        #region Fields
        private MarketTemplateGroupResponse _marketTemplateGroupResponse { get; set; }
        private MarketLookupResponse _marketLookup { get; set; }
        #endregion


        #region Methods

        protected override async Task OnInitializedAsync()
        {
            IsPreloader = true; 

            ConfigTemplate = new ConfigTemplateModel();

            ConfigTemplateMargin = new ConfigTemplateMarginModel();

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

        }

        public async Task InitConfigEditTemplate(EditConfigTemplateFilterModel model)
        {
            IsPreloader = true;
            await InvokeAsync(StateHasChanged);

            EditConfigTemplateFilterModel = model;

            await LoadConfigTemplateById();
            await LoadConfigTemplateMargin();
            await LoadMarkets();

            IsPreloader = false;
        }

        public void Dispose()
        {
            
        }

        #endregion

        #region Utilities      

        private async Task LoadConfigTemplateById()
        {
            var response = await BackOfficeService.GetConfigTemplateById(ConfigTemplateId);

            if (response.StatusCode == StatusCode.Success)
            {
                ConfigTemplate = response.Result;
                
                SportName = (await TradingService.GetSportLookup()).Result.SportLookup.FirstOrDefault(e => e.SportId == response.Result.SportId).SportName;
                SportId = response.Result.SportId;
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
            var response = await BackOfficeService.GetConfigTemplateMargin(ConfigTemplateId);

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
                var marketTemplateGroup = MarketTemplateGroup.MarketTemplateGroup;
                foreach (var marketLookup in _marketLookup.MarketLookup)
                {
                    if (marketTemplateGroup.MarketTemplateIds.Contains(marketLookup.MarketTemplateId))
                    {

                        var newMarketTemplate = marketTemplates.FirstOrDefault(m => m.MarketLookup.MarketTemplateId == marketLookup.MarketTemplateId);
                        if(newMarketTemplate == null)
                        {
                            
                            var configTemplateMargin = ConfigTemplateMargin.ConfigTemplateMargins.FirstOrDefault(c => c.MarketTemplateId == marketLookup.MarketTemplateId);
                            if (configTemplateMargin == null)
                            {
                                configTemplateMargin = new Backoffice.Shared.Models.ConfigTemplateMargin()
                                {
                                    MarketTemplateId = marketLookup.MarketTemplateId,
                                    IsActive = true,
                                    FixtureTypeId = ScopeId,
                                    SportId = SportId
                                };
                            }

                            var prematchSettings = GetPrematchSettingsTemplate(configTemplateMargin, marketLookup.MarketTemplateId);
                            var liveSettings = GetLiveSettingsTemplate(configTemplateMargin, marketLookup.MarketTemplateId);


                            marketTemplates.Add(new MarketLookupNavigationModel()
                            {
                                IsSelected = false,
                                MarketTemplateGroupIds = new List<int>() { marketTemplateGroup.MarketTemplateGroupId },
                                MarketLookup = marketLookup,
                                LiveSettings = liveSettings,
                                PrematchSettings = prematchSettings
                            });
                        }
                        else
                        {
                            if (!newMarketTemplate.MarketTemplateGroupIds.Contains(marketTemplateGroup.MarketTemplateGroupId))
                                newMarketTemplate.MarketTemplateGroupIds.Add(marketTemplateGroup.MarketTemplateGroupId);
                        }

                    }
                }
            }
            return marketTemplates;
        }

        private PrematchSettingsTemplateModel GetPrematchSettingsTemplate(Backoffice.Shared.Models.ConfigTemplateMargin configTemplateMargin, string marketTemplateId)
        {
            return new PrematchSettingsTemplateModel()
            {
                SportId = ConfigTemplate.SportId,
                FirstKey = GetFirstKey(configTemplateMargin, true),
                LastKey = GetLastKey(configTemplateMargin, true),
                MarketTemplateId = marketTemplateId.ToString(),
                Settings = MockData.PrematchSettings.Select(s => new Backoffice.Shared.Models.KeyTimeModel()
                {
                    Key = GetPrematchKey(s.TimeDisplay, configTemplateMargin),
                    Time = s.Time,
                    SortOrder = s.SortOrder,
                    TimeDisplay = s.TimeDisplay
                }).ToList()
            };
        }

        public LiveSettingsTemplateModel GetLiveSettingsTemplate(Backoffice.Shared.Models.ConfigTemplateMargin configTemplateMargin, string marketTemplateId)
        {
            var liveSettings = MockData.SportsLiveTimeline.Where(s => s.SportId == configTemplateMargin.SportId).FirstOrDefault();

            List<KeyTimeModel> keyTime = new List<KeyTimeModel>();

            if (liveSettings != null)
            {
                foreach (var time in liveSettings.Time)
                {
                    keyTime.Add(new KeyTimeModel { Key = GetLiveKey(time.ToString(), configTemplateMargin), TimeDisplay = time.ToString(), Time = time });
                }
            }
            else
            {
                keyTime.Add(new KeyTimeModel { Key = GetLiveKey("0", configTemplateMargin), TimeDisplay = "0" });
            }

            return new LiveSettingsTemplateModel()
            {
                SportId = configTemplateMargin.SportId,
                FirstKey = GetFirstKey(configTemplateMargin, false),
                LastKey = GetLastKey(configTemplateMargin, false),
                MarketTemplateId = configTemplateMargin.MarketTemplateId.ToString(),
                Settings = keyTime,
                OvertimeKey = liveSettings?.Overtime.ToString(),
                LastTimeKey = liveSettings?.Time.Max().ToString()
            };
        }

        private string GetFirstKey(Backoffice.Shared.Models.ConfigTemplateMargin configTemplateMargin, bool isPrematch)
        {

            if (configTemplateMargin == null || (string.IsNullOrEmpty(configTemplateMargin.Prematch) && string.IsNullOrEmpty(configTemplateMargin.Live))) return "101";

            if ((isPrematch && configTemplateMargin.Prematch?.Length == 0)
                || (!isPrematch && configTemplateMargin.Live?.Length == 0)) return "101";

            List<MarketMargin> keyTimes = JsonSerializer.Deserialize<List<MarketMargin>>(isPrematch ? configTemplateMargin.Prematch : configTemplateMargin.Live);

            if (keyTimes == null) return "";

            var firstKey = keyTimes.Where(s => s.M >= 101).FirstOrDefault()?.M;
            return (firstKey ?? 101).ToString();
        }

        private string GetLastKey(Backoffice.Shared.Models.ConfigTemplateMargin configTemplateMargin, bool isPrematch)
        {
            if (configTemplateMargin == null || (string.IsNullOrEmpty(configTemplateMargin.Prematch) && string.IsNullOrEmpty(configTemplateMargin.Live))) return "";

            if ((isPrematch && configTemplateMargin.Prematch?.Length == 0)
                || (!isPrematch && configTemplateMargin.Live?.Length == 0)) return "";

            List<MarketMargin> keyTimes = JsonSerializer.Deserialize<List<MarketMargin>>(isPrematch ? configTemplateMargin.Prematch : configTemplateMargin.Live);
            if (keyTimes == null) return "";

            var lastKey = keyTimes.Where(s => s.M > 101).LastOrDefault()?.M;
            return lastKey == null ? "" : lastKey.ToString();
        }

        private int? GetPrematchKey(string Time, Backoffice.Shared.Models.ConfigTemplateMargin configTemplateMargin)
        {

            if (configTemplateMargin == null)
            {
                if (Time == "INST")
                    return 101;
                else
                    return null;
            }

            if (string.IsNullOrEmpty(configTemplateMargin.Prematch))
            {
                if (Time == "INST")
                    return 101;
                else
                    return null;
            }


            List<MarketMargin> keyTimes = JsonSerializer.Deserialize<List<MarketMargin>>(configTemplateMargin.Prematch);
            if (keyTimes == null && Time == "INST")
                return 101;

            if (keyTimes == null)
                return null;

            var timeDisplay = MockData.PrematchSettings.Where(a => a.TimeDisplay == Time).FirstOrDefault();
            if (timeDisplay == null) return null;
            var keyTime = keyTimes.Where(a => a.T == timeDisplay.Time).FirstOrDefault();
            if (keyTime == null) return null;
            return keyTime.M;
        }

        private int? GetLiveKey(string Time, Backoffice.Shared.Models.ConfigTemplateMargin configTemplateMargin)
        {
            ////for testing only
            //if (config == null || string.IsNullOrEmpty(config.Live))
            //{
            //    if (ConfigFilterModel.ClientCode == "10C" || ConfigFilterModel.ClientCode == "10I")
            //        if (Time == "0")
            //            return 112;
            //    if (ConfigFilterModel.ClientCode == "10J")
            //        if (Time == "0")
            //            return 109;
            //}
            ////end

            if (configTemplateMargin == null)
            {
                if (Time == "0")
                    return 101;
                else
                    return null;
            }

            if (string.IsNullOrEmpty(configTemplateMargin.Live))
            {
                if (Time == "0")
                    return 101;
                else
                    return null;
            }

            List<MarketMargin> keyTimes = JsonSerializer.Deserialize<List<MarketMargin>>(configTemplateMargin.Live);
            if (keyTimes == null && Time == "0")
                return 101;

            if (keyTimes == null)
                return null;

            var keyTime = keyTimes.Where(a => a.T == int.Parse(Time)).FirstOrDefault();
            if (keyTime == null) return null;
            return keyTime.M;

        }

        #endregion
    }
}
