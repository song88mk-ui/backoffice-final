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


        public ConfigTemplateMarginModel ConfigTemplateMargin { get; set; }
        public EditConfigTemplateFilterModel EditConfigTemplateFilterModel { get; set; }
        public int SelectedClientCount { get; set; }
        public string SelectedClientName { get; set; } = "";
        public int SelectedLeagueCount { get; set; }
        public string SelectedLeagueName { get; set; } = "";
        public string SportIcon { get; set; } = "";
        private string ConfigTemplateName { get; set; } = "";
        private string SportName { get; set; } = "";
        private string Scope { get; set; } = "";

        public bool IsPreloader { get; set; } = true;
        #endregion


        #region Methods

        protected override async Task OnInitializedAsync()
        {
            IsPreloader = true;

            ConfigTemplateMargin = new ConfigTemplateMarginModel();

            MarketLookupNavigation = new List<MarketLookupNavigationModel>();
            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = $"hk/odds-engine/config-templates",
                    IsCurrentPage = false
                },
            };
        }

        public async Task InitConfigEditTemplate(EditConfigTemplateFilterModel model)
        {
            IsPreloader = true;
            await InvokeAsync(StateHasChanged);

            EditConfigTemplateFilterModel = model;

            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = $"hk/odds-engine/config-templates",
                    IsCurrentPage = false
                },
                new BreadcrumbModel
                {
                    Name = $"{EditConfigTemplateFilterModel.ConfigTemplate.Name}",
                    Url = $"hk/odds-engine/config-templates/edit/{ConfigTemplateId}",
                    IsCurrentPage = true
                }
            };

            SportIcon = EditConfigTemplateFilterModel.SportName.Contains(" ") ? EditConfigTemplateFilterModel.SportName.Replace(" ", "-").ToLower() : EditConfigTemplateFilterModel.SportName.ToLower();
            SportName = EditConfigTemplateFilterModel.SportName;
            ConfigTemplateName = EditConfigTemplateFilterModel.ConfigTemplate.Name;
            Scope = ((FixtureStatus)EditConfigTemplateFilterModel.ScopeId).ToString();

            await LoadConfigTemplateMargin();
            await LoadMarkets();

            IsPreloader = false;
        }

        public void Dispose()
        {

        }

        #endregion

        #region Utilities      


        private async Task LoadConfigTemplateMargin()
        {
            var response = await BackOfficeService.GetConfigTemplateMargin(ConfigTemplateId);

            if (response.StatusCode == StatusCode.Success)
                ConfigTemplateMargin = response.Result;
        }


        private async Task LoadMarkets()
        {

            if (EditConfigTemplateFilterModel.MarketTemplateGroups.Count > 0)
            {
                MarketLookupNavigation = await GetMarketTemplate(EditConfigTemplateFilterModel.MarketTemplateGroups);
            }
        }


        private async Task<List<MarketLookupNavigationModel>> GetMarketTemplate(List<MarketGroupNavigationModel> MarketTemplateGroups)
        {
            var marketTemplates = new List<MarketLookupNavigationModel>();
            foreach (var MarketTemplateGroup in MarketTemplateGroups)
            {
                if (!EditConfigTemplateFilterModel.SelectedMarkets.Contains(MarketTemplateGroup.MarketTemplateGroup.MarketTemplateGroupId.ToString()))
                    continue;

                var marketTemplateGroup = MarketTemplateGroup.MarketTemplateGroup;
                foreach (var marketLookup in EditConfigTemplateFilterModel.MarketLookup.MarketLookup)
                {
                    if (marketTemplateGroup.MarketTemplateIds.Contains(marketLookup.MarketTemplateId))
                    {

                        var newMarketTemplate = marketTemplates.FirstOrDefault(m => m.MarketLookup.MarketTemplateId == marketLookup.MarketTemplateId);
                        if (newMarketTemplate == null)
                        {

                            var configTemplateMargin = ConfigTemplateMargin.ConfigTemplateMargins.FirstOrDefault(c => c.MarketTemplateId == marketLookup.MarketTemplateId);
                            if (configTemplateMargin == null)
                            {
                                configTemplateMargin = new Backoffice.Shared.Models.ConfigTemplateMargin()
                                {
                                    MarketTemplateId = marketLookup.MarketTemplateId,
                                    IsActive = true,
                                    FixtureTypeId = EditConfigTemplateFilterModel.ScopeId,
                                    SportId = EditConfigTemplateFilterModel.SportId
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
                                NewLiveSettings = liveSettings,
                                PrematchSettings = prematchSettings,
                                NewPrematchSettings = prematchSettings
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
                SportId = EditConfigTemplateFilterModel.ConfigTemplate.SportId,
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
