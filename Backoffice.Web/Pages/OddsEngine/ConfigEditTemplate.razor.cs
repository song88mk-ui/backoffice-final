using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using Activity = Backoffice.Shared.Enums.Activity;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ConfigEditTemplate : IDisposable
    {
        #region Properties
        [Inject] public NavigationManager NavigationManager { get; set; }

        [Inject] private IBackofficeService BackofficeService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [Parameter] public string Region { get; set; } = "hk";
        [Parameter] public long ConfigTemplateId { get; set; }
        [Parameter] public string Action { get; set; }
        [Parameter] public bool View { get; set; }

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
        private string AllMarketStatusName { get; set; } = "ACTIVATE ALL";
        public bool IsPreloader { get; set; } = true;
        public bool HasChanges { get; set; } = false;
        public bool IsNameChanges { get; set; } = false;
        private AlertModel _alertModel { get; set; }
        private Activity? _activity;
        private bool _isExisitngName;
        private bool _isProcessing;
        private bool _isEdttingName;
        private string _temporaryName;
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

            _activity = Action.ToUpper() switch
            {
                "EDIT" => Activity.Update,
                "COPY" => Activity.Copy,
                _ => null
            };

            if (_activity == null)
            {
                NavigationManager.NavigateTo("/notfound");
                return;
            }

            HasChanges = _activity == Activity.Copy;

            await LoadConfigTemplateMargin();
        }
        protected override Task OnParametersSetAsync()
        {
            return base.OnParametersSetAsync();
        }
        public async Task InitConfigEditTemplate(EditConfigTemplateFilterModel model)
        {
            IsPreloader = true;

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
            ConfigTemplateName = $"{(_activity == Activity.Copy ? "Copy of " : "")}{EditConfigTemplateFilterModel.ConfigTemplate.Name}";
            Scope = ((FixtureStatus)EditConfigTemplateFilterModel.ScopeId).ToString();

            await LoadMarkets();

            AllMarketStatusName = MarketLookupNavigation.Any(m => m.IsVisible && m.NewMarketLookup.IsActive == false) ? "ACTIVATE ALL" : "DEACTIVATE ALL";

            IsPreloader = false;
        }

        public async Task OnAllMarketStatusClickEvent()
        {

            if (AllMarketStatusName == "ACTIVATE ALL")
            {
                MarketLookupNavigation.Where(m => m.IsVisible).ToList().ForEach(market => market.NewMarketLookup.IsActive = true);
            }
            else
            {
                MarketLookupNavigation.Where(m => m.IsVisible).ToList().ForEach(market => market.NewMarketLookup.IsActive = false);
            }

            AllMarketStatusName = AllMarketStatusName == "DEACTIVATE ALL" ? "ACTIVATE ALL" : "DEACTIVATE ALL";

            await OnTemplateMarginChange();
        }

        public async Task OnTemplateMarginChange()
        {
            if (_activity == Activity.Copy)
            {
                HasChanges = true;
            }
            else
            {
                foreach (var market in MarketLookupNavigation)
                {
                    HasChanges = !DeepCompare(market.MarketLookup, market.NewMarketLookup);
                    if (HasChanges)
                        break;

                    HasChanges = !DeepCompare(market.PrematchSettings, market.NewPrematchSettings);
                    if (HasChanges)
                        break;

                    HasChanges = !DeepCompare(market.LiveSettings, market.NewLiveSettings);
                    if (HasChanges)
                        break;

                }
            }
        }

        public async Task OnSaveMarginTemplate()
        {
            if (_isProcessing)
                return;

            _isProcessing = true;

            try
            {
                var referenceConfigTemplateId = ConfigTemplateId;

                if (HasChanges || _activity == Activity.Copy)
                {
                    ConfigTemplateMarginModel request = new ConfigTemplateMarginModel();

                    var activityLog = new List<ActivityLogModel>();
                    var oldConfigMarginsLogs = new List<ConfigMarginActivityLogModel>();
                    var newConfigMarginsLogs = new List<ConfigMarginActivityLogModel>();
                    var oldMarketState = new List<ConfigMarginActivityLogModel>();
                    var newMarketState = new List<ConfigMarginActivityLogModel>();

                    foreach (var market in MarketLookupNavigation)
                    {
                        //validate Odds
                        if (market.NewMarketLookup.IsActive)
                        {
                            switch ((FixtureStatus)EditConfigTemplateFilterModel.ScopeId)
                            {
                                case FixtureStatus.Prematch:
                                    if (market.NewPrematchSettings.Settings.Any(x => x.Key <= 101))
                                    {
                                        _alertModel = new AlertModel();
                                        _alertModel.Message = "Please update margin for Active markets. 101 by default is not allowed.";
                                        _alertModel.Status = AlertStatus.Error;
                                        _alertModel.Opacity = 1.0m;

                                        return;
                                    }
                                    break;
                                case FixtureStatus.Live:
                                    if (market.NewLiveSettings.Settings.Any(x => x.Key <= 101))
                                    {
                                        _alertModel = new AlertModel();
                                        _alertModel.Message = "Please update margin for Active markets. 101 by default is not allowed.";
                                        _alertModel.Status = AlertStatus.Error;
                                        _alertModel.Opacity = 1.0m;

                                        return;
                                    }
                                    break;
                            }
                        }

                        var livematchSettings = new List<MarketMargin>();
                        var liveSettings = market.NewLiveSettings.Settings.Where(a => a.Key != null);
                        if (liveSettings != null)
                            livematchSettings = liveSettings.Select(a => new MarketMargin { T = a.Time, M = (int)a.Key }).ToList();
                        else
                            livematchSettings = new List<MarketMargin>();

                        var prematchSettings = new List<MarketMargin>();
                        var preSettings = market.NewPrematchSettings.Settings.Where(a => a.Key != null);
                        if (preSettings != null)
                            prematchSettings = preSettings.Select(a => new MarketMargin { T = a.Time, M = (int)a.Key }).ToList();
                        else
                            prematchSettings = new List<MarketMargin>();

                        switch ((FixtureStatus)EditConfigTemplateFilterModel.ScopeId)
                        {
                            case FixtureStatus.Prematch:
                                if (market.NewPrematchSettings.Settings.Any(x => market.PrematchSettings.Settings.Any(f => f.Time == x.Time && f.Key != x.Key)))
                                {
                                    oldConfigMarginsLogs.Add(new ConfigMarginActivityLogModel()
                                    {
                                        MarketTemplateId = market.MarketLookup.MarketTemplateId,
                                        MarketTemplateName = market.MarketLookup.MarketTemplateName,
                                        Payload = market.PrematchSettings.Settings.Where(x => market.NewPrematchSettings.Settings.Any(f => f.Time == x.Time && f.Key != x.Key)).Select(x => $"{x.TimeDisplay} {(_activity == Activity.Copy ? "101" : x.Key)}").ToList()
                                    });
                                    newConfigMarginsLogs.Add(new ConfigMarginActivityLogModel()
                                    {
                                        MarketTemplateId = market.MarketLookup.MarketTemplateId,
                                        MarketTemplateName = market.MarketLookup.MarketTemplateName,
                                        Payload = market.NewPrematchSettings.Settings.Where(x => market.PrematchSettings.Settings.Any(f => f.Time == x.Time && f.Key != x.Key)).Select(x => $"{x.TimeDisplay} {x.Key}").ToList()
                                    });
                                }
                                break;
                            case FixtureStatus.Live:
                                if (market.NewLiveSettings.Settings.Any(x => market.LiveSettings.Settings.Any(f => f.Time == x.Time && f.Key != x.Key)))
                                {
                                    oldConfigMarginsLogs.Add(new ConfigMarginActivityLogModel()
                                    {
                                        MarketTemplateId = market.MarketLookup.MarketTemplateId,
                                        MarketTemplateName = market.MarketLookup.MarketTemplateName,
                                        Payload = market.LiveSettings.Settings.Where(x => market.NewLiveSettings.Settings.Any(f => f.Time == x.Time && f.Key != x.Key)).Select(x => $"{x.TimeDisplay} {(_activity == Activity.Copy ? "101" : x.Key)}").ToList()
                                    });
                                    newConfigMarginsLogs.Add(new ConfigMarginActivityLogModel()
                                    {
                                        MarketTemplateId = market.MarketLookup.MarketTemplateId,
                                        MarketTemplateName = market.MarketLookup.MarketTemplateName,
                                        Payload = market.NewLiveSettings.Settings.Where(x => market.LiveSettings.Settings.Any(f => f.Time == x.Time && f.Key != x.Key)).Select(x => $"{x.TimeDisplay} {x.Key}").ToList()
                                    });
                                }
                                break;
                        }

                        if (market.MarketLookup.IsActive != market.NewMarketLookup.IsActive)
                        {
                            oldMarketState.Add(new ConfigMarginActivityLogModel()
                            {
                                MarketTemplateId = market.MarketLookup.MarketTemplateId,
                                MarketTemplateName = market.MarketLookup.MarketTemplateName,
                                Payload = new List<string>() { market.MarketLookup.IsActive ? "Active" : "Inactive" }
                            });

                            newMarketState.Add(new ConfigMarginActivityLogModel()
                            {
                                MarketTemplateId = market.MarketLookup.MarketTemplateId,
                                MarketTemplateName = market.MarketLookup.MarketTemplateName,
                                Payload = new List<string>() { market.NewMarketLookup.IsActive ? "Active" : "Inactive" }
                            });
                        }

                        request.ConfigTemplateMargins.Add(new ConfigTemplateMargin
                        {
                            ConfigTemplateId = referenceConfigTemplateId,
                            SportId = EditConfigTemplateFilterModel.SportId,
                            MarketTemplateId = market.MarketLookup.MarketTemplateId,
                            FixtureTypeId = (int)FixtureType.Match,
                            FixtureStatusId = EditConfigTemplateFilterModel.ScopeId,
                            IsActive = market.NewMarketLookup.IsActive,
                            Config = EditConfigTemplateFilterModel.ScopeId == (int)FixtureStatus.Prematch ? JsonSerializer.Serialize(prematchSettings) : JsonSerializer.Serialize(livematchSettings)
                        });
                    }

                    if (_activity == Activity.Copy)
                    {
                        _isExisitngName = false;
                        var payload = new ConfigTemplateModel()
                        {
                            SportId = EditConfigTemplateFilterModel.SportId,
                            FixtureTypeId = (int)FixtureType.Match,
                            FixtureStatusId = EditConfigTemplateFilterModel.ScopeId,
                            Name = ConfigTemplateName,
                        };

                        var response = await BackofficeService.AddOrUpdateConfigTemplate(payload);

                        if (response.StatusCode == StatusCode.Success)
                        {
                            await BackofficeService.AddActivityLog(new ActivityLogModel()
                            {
                                Page = SportsbookClientPermission.SBC_OE_Config.ToString().Replace("_", "."),
                                Payload = $"{EditConfigTemplateFilterModel.SportId}.{EditConfigTemplateFilterModel.ScopeId}.{payload.Name}.T".Replace(" ", "_"),
                                From = "-",
                                To = JsonSerializer.Serialize(payload),
                                Activity = Activity.Create.ToString(),
                                UserId = UserModel.Id,
                                UserName = UserModel.UserName,
                                TimeStamp = DateTime.UtcNow
                            });

                            referenceConfigTemplateId = response.Result;
                        }
                        else
                        {
                            _isExisitngName = true;
                            return;
                        }

                        request.ConfigTemplateMargins.ForEach(x =>
                        {
                            x.ConfigTemplateId = referenceConfigTemplateId;
                        });
                    }

                    var result = await BackofficeService.AddOrUpdateConfigTemplateMargin(request);
                    if (result.StatusCode == StatusCode.Success)
                    {
                        var configTemplateMargins = await GetConfigTemplateMargin(referenceConfigTemplateId);

                        var assignedLeagues = await LoadLeagues((int)referenceConfigTemplateId);
                        foreach (var league in assignedLeagues)
                        {
                            ConfigSettingsModel data = new ConfigSettingsModel();
                            data.ClientCode = league.ClientCode;
                            data.SportId = league.SportId;
                            data.SegmentId = league.SegmentId;
                            data.LeagueId = league.LeagueId;
                            data.ClientId = Guid.Parse(league.ClientId);
                            data.UserId = UserModel.Id;
                            data.UserName = UserModel.UserName;


                            var marketConfigs = new List<MarketConfig>();
                            var newConfigTemplateMargins = EditConfigTemplateFilterModel.ScopeId == (int)FixtureStatus.Prematch ? await GetConfigTemplateMargin(league.LiveTemplateId) : await GetConfigTemplateMargin(league.PrematchTemplateId);

                            foreach (var margin in configTemplateMargins.ConfigTemplateMargins)
                            {
                                var marketConfig = new MarketConfig()
                                {
                                    MarketTemplateId = margin.MarketTemplateId,
                                    FixtureTypeId = (int)FixtureType.Match
                                };

                                if (EditConfigTemplateFilterModel.ScopeId == (int)FixtureStatus.Prematch)
                                {
                                    marketConfig.Prematch = margin.Config;
                                    marketConfig.Live = newConfigTemplateMargins.ConfigTemplateMargins.FirstOrDefault(f => f.MarketTemplateId == margin.MarketTemplateId)?.Config ?? string.Empty;
                                    marketConfig.IsActive = !string.IsNullOrEmpty(marketConfig.Live) && league.LiveTemplateId > 0 ? true : margin.IsActive;
                                }

                                if (EditConfigTemplateFilterModel.ScopeId == (int)FixtureStatus.Live)
                                {
                                    marketConfig.Live = margin.Config;
                                    marketConfig.Prematch = newConfigTemplateMargins.ConfigTemplateMargins.FirstOrDefault(f => f.MarketTemplateId == margin.MarketTemplateId)?.Config ?? string.Empty;
                                    marketConfig.IsActive = !string.IsNullOrEmpty(marketConfig.Prematch) && league.PrematchTemplateId > 0 ? true : margin.IsActive;
                                }

                                marketConfigs.Add(marketConfig);
                            }

                            data.MarketConfig = marketConfigs.Select(s => new MarketConfigModel
                            {
                                IsActive = s.IsActive,
                                MarketTemplateId = s.MarketTemplateId,
                                PrematchSettings = string.IsNullOrEmpty(s.Prematch) ? new List<MarketMargin>() { new MarketMargin { T = -41472000, M = -1 } } : JsonSerializer.Deserialize<List<MarketMargin>>(s.Prematch)?.ToList() ?? new List<MarketMargin>(),
                                LivematchSettings = string.IsNullOrEmpty(s.Live) ? new List<MarketMargin>() { new MarketMargin { T = 0, M = -1 } } : JsonSerializer.Deserialize<List<MarketMargin>>(s.Live)?.ToList() ?? new List<MarketMargin>(),
                            }).ToList();

                            data.LeagueConfig = new List<LeagueConfigModel>()
                        {
                            new LeagueConfigModel
                            {
                                IsActive = data.MarketConfig.Any(m => m.IsActive),
                                LeagueId = league.LeagueId,
                                SegmentId = league.SegmentId
                            }
                        };

                            var tresult = await TradingService.AddOrUpdateConfig(data);
                            if (tresult.StatusCode == StatusCode.Success)
                            {

                                var messageLeague = new OnLeagueConfigMessage
                                {
                                    ClientCode = league.ClientCode,
                                    ClientId = Guid.Parse(league.ClientId),
                                    SportId = league.SportId,
                                    SegmentId = league.SegmentId,
                                    LeagueId = league.LeagueId,
                                    IsActive = data.MarketConfig.Any(m => m.IsActive)
                                };

                                await TradingService.Publish(new MessageRequest()
                                {
                                    CorrelationId = Guid.NewGuid().ToString(),
                                    MessageType = "OnLeagueConfig",
                                    RoutingKey = $"r.uof.onleagueconfig",
                                    Payload = JsonSerializer.Serialize(messageLeague)
                                });

                                var messageMarket = new OnMarketConfigMessage
                                {
                                    ClientCode = league.ClientCode,
                                    ClientId = Guid.Parse(league.ClientId),
                                    SportId = league.SportId,
                                    SegmentId = league.SegmentId,
                                    LeagueId = league.LeagueId,
                                    Markets = marketConfigs
                                };

                                await TradingService.Publish(new MessageRequest()
                                {
                                    CorrelationId = Guid.NewGuid().ToString(),
                                    MessageType = "OnMarketConfig",
                                    RoutingKey = $"r.uof.onmarketconfig",
                                    Payload = JsonSerializer.Serialize(messageMarket)
                                });

                            }
                        }

                        if (new[] { oldConfigMarginsLogs, newConfigMarginsLogs }.All(x => x != null && x.Any()) && !oldConfigMarginsLogs.Equals(newConfigMarginsLogs))
                        {
                            await BackofficeService.AddActivityLog(new ActivityLogModel()
                            {
                                Page = SportsbookClientPermission.SBC_OE_Config.ToString().Replace("_", "."),
                                Payload = $"{EditConfigTemplateFilterModel.SportId}.{EditConfigTemplateFilterModel.ScopeId}.{EditConfigTemplateFilterModel.ConfigTemplate.Name}.Modified_Margins.M".Replace(" ", "_"),
                                From = _activity == Activity.Copy ?  "-" : JsonSerializer.Serialize(oldConfigMarginsLogs),
                                To = JsonSerializer.Serialize(newConfigMarginsLogs),
                                Activity = Activity.Update.ToString(),
                                UserId = UserModel.Id,
                                UserName = UserModel.UserName,
                                TimeStamp = DateTime.UtcNow
                            });
                        }
                        if (new[] { oldMarketState, newMarketState }.All(x => x != null && x.Any()) && !oldMarketState.Equals(newMarketState))
                        {
                            await BackofficeService.AddActivityLog(new ActivityLogModel()
                            {
                                Page = SportsbookClientPermission.SBC_OE_Config.ToString().Replace("_", "."),
                                Payload = $"{EditConfigTemplateFilterModel.SportId}.{EditConfigTemplateFilterModel.ScopeId}.{EditConfigTemplateFilterModel.ConfigTemplate.Name}.Changed_Market_State.M".Replace(" ", "_"),
                                From = _activity == Activity.Copy ? "-" : JsonSerializer.Serialize(oldMarketState),
                                To = JsonSerializer.Serialize(newMarketState),
                                Activity = Activity.Update.ToString(),
                                UserId = UserModel.Id,
                                UserName = UserModel.UserName,
                                TimeStamp = DateTime.UtcNow
                            });
                        }

                        NavigationManager.NavigateTo($"hk/odds-engine/config-templates?message=Successfully updated");
                    }
                }


                if (IsNameChanges && _activity != Activity.Copy)
                {
                    var payload = new ConfigTemplateModel()
                    {
                        Id = ConfigTemplateId,
                        SportId = EditConfigTemplateFilterModel.SportId,
                        FixtureTypeId = (int)FixtureType.Match,
                        FixtureStatusId = EditConfigTemplateFilterModel.ScopeId,
                        Name = ConfigTemplateName,
                    };

                    var response = await BackofficeService.AddOrUpdateConfigTemplate(payload);
                    IsNameChanges = false;
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                _isProcessing = false;
            }
        }

        public async Task OnCancelMarginTemplate()
        {
            NavigationManager.NavigateTo($"hk/odds-engine/config-templates");
        }
        public async Task OnTemplateNameClicked()
        {
            _isEdttingName = true;
            _temporaryName = ConfigTemplateName;
        }
        public async Task OnSaveTemplateNameClicked()
        {
            _isEdttingName = false;
            ConfigTemplateName = _temporaryName;
            _isExisitngName = false;

            if (ConfigTemplateName != EditConfigTemplateFilterModel.ConfigTemplate.Name)
                IsNameChanges = true;
        }
        public async Task OnCancelTemplateNameClicked()
        {
            _isEdttingName = false;
        }

        public void Dispose()
        {

        }

        #endregion

        #region Utilities      


        private async Task LoadConfigTemplateMargin()
        {
            var response = await BackofficeService.GetConfigTemplateMargin(ConfigTemplateId);

            if (response.StatusCode == StatusCode.Success)
                ConfigTemplateMargin = response.Result;
        }


        private async Task LoadMarkets()
        {
            if (EditConfigTemplateFilterModel.MarketTemplateGroups.Count > 0)
            {
                await GetMarketTemplate(EditConfigTemplateFilterModel.MarketTemplateGroups);
            }
        }


        private async Task GetMarketTemplate(List<MarketGroupNavigationModel> MarketTemplateGroups)
        {

            foreach (var MarketTemplateGroup in MarketTemplateGroups)
            {
                //if (!EditConfigTemplateFilterModel.SelectedMarkets.Contains(MarketTemplateGroup.MarketTemplateGroup.MarketTemplateGroupId.ToString()))
                //    continue;

                var marketTemplateGroup = MarketTemplateGroup.MarketTemplateGroup;
                foreach (var marketLookup in EditConfigTemplateFilterModel.MarketLookup.MarketLookup)
                {
                    if (marketTemplateGroup.MarketTemplateIds.Contains(marketLookup.MarketTemplateId))
                    {
                        //if (!(string.IsNullOrEmpty(EditConfigTemplateFilterModel.SearchText) || marketLookup.MarketTemplateName.Contains(EditConfigTemplateFilterModel.SearchText, StringComparison.OrdinalIgnoreCase)))
                        //    continue;

                        var isVisible = (((EditConfigTemplateFilterModel.SelectedMarkets.Any() || MarketTemplateGroups.Count == 1) && MarketTemplateGroup.MarketTemplateGroup.MarketTemplateGroupId == 1)
                                || EditConfigTemplateFilterModel.SelectedMarkets.Contains(MarketTemplateGroup.MarketTemplateGroup.MarketTemplateGroupId.ToString()))
                                                && (string.IsNullOrEmpty(EditConfigTemplateFilterModel.SearchText) || marketLookup.MarketTemplateName.Contains(EditConfigTemplateFilterModel.SearchText, StringComparison.OrdinalIgnoreCase))
                                                && EditConfigTemplateFilterModel.SelectedConfigStatus.Contains(marketLookup.IsActive ? "1" : "2");

                        var newMarketTemplate = MarketLookupNavigation.FirstOrDefault(m => m.MarketLookup.MarketTemplateId == marketLookup.MarketTemplateId);
                        if (newMarketTemplate == null)
                        {

                            var configTemplateMargin = ConfigTemplateMargin.ConfigTemplateMargins.FirstOrDefault(c => c.MarketTemplateId == marketLookup.MarketTemplateId);
                            if (configTemplateMargin == null)
                            {
                                configTemplateMargin = new Backoffice.Shared.Models.ConfigTemplateMargin()
                                {
                                    MarketTemplateId = marketLookup.MarketTemplateId,
                                    IsActive = false,
                                    FixtureStatusId = EditConfigTemplateFilterModel.ScopeId,
                                    FixtureTypeId = (int)FixtureType.Match,
                                    SportId = EditConfigTemplateFilterModel.SportId
                                };
                            }
                            else
                            {
                                marketLookup.IsActive = configTemplateMargin.IsActive;
                            }

                            //if (!EditConfigTemplateFilterModel.SelectedConfigStatus.Contains(marketLookup.IsActive ? "1" : "2"))
                            //    continue;

                            var prematchSettings = GetPrematchSettingsTemplate(configTemplateMargin, marketLookup.MarketTemplateId, marketLookup.MarketTemplateName);
                            var liveSettings = GetLiveSettingsTemplate(configTemplateMargin, marketLookup.MarketTemplateId);


                            MarketLookupNavigation.Add(new MarketLookupNavigationModel()
                            {
                                IsVisible = isVisible,
                                IsSelected = false,
                                MarketTemplateGroupIds = new List<int>() { marketTemplateGroup.MarketTemplateGroupId },
                                MarketLookup = marketLookup,
                                NewMarketLookup = new MarketLookupModel
                                {
                                    IsActive = marketLookup.IsActive,
                                    MarketTemplateId = marketLookup.MarketTemplateId,
                                    MarketTemplateName = marketLookup.MarketTemplateName
                                },
                                LiveSettings = liveSettings,
                                NewLiveSettings = new LiveSettingsTemplateModel
                                {
                                    MarketTemplateId = liveSettings.MarketTemplateId,
                                    FirstKey = liveSettings.FirstKey,
                                    LastKey = liveSettings.LastKey,
                                    LastTimeKey = liveSettings.LastTimeKey,
                                    OvertimeKey = liveSettings.OvertimeKey,
                                    SportId = liveSettings.SportId,
                                    Settings = liveSettings.Settings.Select(s => new KeyTimeModel
                                    {
                                        IsTooltipVisible = s.IsTooltipVisible,
                                        Key = s.Key,
                                        SortOrder = s.SortOrder,
                                        Time = s.Time,
                                        TimeDisplay = s.TimeDisplay
                                    }).ToList()
                                },
                                PrematchSettings = prematchSettings,
                                NewPrematchSettings = new PrematchSettingsTemplateModel
                                {
                                    FirstKey = prematchSettings.FirstKey,
                                    LastKey = prematchSettings.LastKey,
                                    MarketTemplateId = prematchSettings.MarketTemplateId,
                                    SportId = prematchSettings.SportId,
                                    Settings = prematchSettings.Settings.Select(s => new KeyTimeModel
                                    {
                                        TimeDisplay = s.TimeDisplay,
                                        IsTooltipVisible = s.IsTooltipVisible,
                                        Key = s.Key,
                                        SortOrder = s.SortOrder,
                                        Time = s.Time
                                    }).ToList()
                                }
                            });
                        }
                        else
                        {
                            newMarketTemplate.IsVisible = isVisible;

                            if (!newMarketTemplate.MarketTemplateGroupIds.Contains(marketTemplateGroup.MarketTemplateGroupId))
                                newMarketTemplate.MarketTemplateGroupIds.Add(marketTemplateGroup.MarketTemplateGroupId);
                        }

                    }
                }
            };
        }

        private PrematchSettingsTemplateModel GetPrematchSettingsTemplate(Backoffice.Shared.Models.ConfigTemplateMargin configTemplateMargin, string marketTemplateId, string marketTemplateName)
        {
            return new PrematchSettingsTemplateModel()
            {
                SportId = EditConfigTemplateFilterModel.ConfigTemplate.SportId,
                FirstKey = GetFirstKey(configTemplateMargin),
                LastKey = GetLastKey(configTemplateMargin),
                MarketTemplateId = marketTemplateId,
                Settings = MockData.PrematchSettings.Select(s => new Backoffice.Shared.Models.KeyTimeModel()
                {
                    Key = GetPrematchKey(s.TimeDisplay, configTemplateMargin),
                    Time = s.Time,
                    SortOrder = s.SortOrder,
                    TimeDisplay = s.TimeDisplay
                }).ToList()
            };
        }

        private async Task<ConfigTemplateMarginModel> GetConfigTemplateMargin(long configTemplateId)
        {
            var response = await BackofficeService.GetConfigTemplateMargin(configTemplateId);

            if (response.StatusCode == StatusCode.Success)
                return response.Result;
            else
                return new ConfigTemplateMarginModel();

        }

        private async Task<List<ConfigClientViewModel>> LoadLeagues(int configTemplateId)
        {
            var result = new List<ConfigClientViewModel>();

            var payload = new ConfigTemplateLeagueRequest();
            payload.MetaData = null;
            payload.SportId = EditConfigTemplateFilterModel.ConfigTemplate.SportId;

            var response = await BackofficeService.GetConfigClient(ConfigState.Assigned, payload, configTemplateId);

            if (response.StatusCode == StatusCode.Success)
            {

                if (response.Result != null)
                {
                    if (response.Result.ClientConfigs != null && response.Result.ClientConfigs.Any())
                        result = ConfigClientViewModel.ConvertList(response.Result.ClientConfigs);

                }
            }
            return result;
        }

        public LiveSettingsTemplateModel GetLiveSettingsTemplate(Backoffice.Shared.Models.ConfigTemplateMargin configTemplateMargin, string marketTemplateId)
        {
            var liveSettings = MockData.SportsLiveTimeline.Where(s => s.SportId == configTemplateMargin.SportId).FirstOrDefault();

            List<KeyTimeModel> keyTime = new List<KeyTimeModel>();

            if (liveSettings != null)
            {
                foreach (var time in liveSettings.Time)
                {
                    keyTime.Add(new KeyTimeModel { Key = GetLiveKey(time.ToString(), configTemplateMargin), TimeDisplay = time.ToString(), Time = (time * 60) });
                }
            }
            else
            {
                keyTime.Add(new KeyTimeModel { Key = GetLiveKey("0", configTemplateMargin), TimeDisplay = "0" });
            }

            return new LiveSettingsTemplateModel()
            {
                SportId = configTemplateMargin.SportId,
                FirstKey = GetFirstKey(configTemplateMargin),
                LastKey = GetLastKey(configTemplateMargin),
                MarketTemplateId = configTemplateMargin.MarketTemplateId.ToString(),
                Settings = keyTime,
                OvertimeKey = liveSettings?.Overtime.ToString(),
                LastTimeKey = liveSettings?.Time.Max().ToString()
            };
        }

        private string GetFirstKey(Backoffice.Shared.Models.ConfigTemplateMargin configTemplateMargin)
        {

            if (configTemplateMargin == null || string.IsNullOrEmpty(configTemplateMargin.Config)) return "101";

            if (configTemplateMargin.Config?.Length == 0) return "101";

            List<MarketMargin> keyTimes = JsonSerializer.Deserialize<List<MarketMargin>>(configTemplateMargin.Config);

            if (keyTimes == null) return "";

            var firstKey = keyTimes.Where(s => s.M >= 101).FirstOrDefault()?.M;
            return (firstKey ?? 101).ToString();
        }

        private string GetLastKey(Backoffice.Shared.Models.ConfigTemplateMargin configTemplateMargin)
        {
            if (configTemplateMargin == null || string.IsNullOrEmpty(configTemplateMargin.Config)) return "101";

            if (configTemplateMargin.Config?.Length == 0) return "";

            List<MarketMargin> keyTimes = JsonSerializer.Deserialize<List<MarketMargin>>(configTemplateMargin.Config);
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

            if (string.IsNullOrEmpty(configTemplateMargin.Config))
            {
                if (Time == "INST")
                    return 101;
                else
                    return null;
            }


            List<MarketMargin> keyTimes = JsonSerializer.Deserialize<List<MarketMargin>>(configTemplateMargin.Config);
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

            if (string.IsNullOrEmpty(configTemplateMargin.Config))
            {
                if (Time == "0")
                    return 101;
                else
                    return null;
            }

            List<MarketMargin> keyTimes = JsonSerializer.Deserialize<List<MarketMargin>>(configTemplateMargin.Config);
            if (keyTimes == null && Time == "0")
                return 101;

            if (keyTimes == null)
                return null;

            var keyTime = keyTimes.Where(a => a.T == int.Parse(Time)).FirstOrDefault();
            if (keyTime == null) return null;
            return keyTime.M;

        }

        protected bool DeepCompare(object obj, object another)
        {
            if (ReferenceEquals(obj, another)) return true;
            if ((obj == null) || (another == null)) return false;
            //Compare two object's class, return false if they are difference
            if (obj.GetType() != another.GetType()) return false;

            var result = true;
            //Get all properties of obj
            //And compare each other
            foreach (var property in obj.GetType().GetProperties())
            {
                var objValue = property.GetValue(obj);
                var anotherValue = property.GetValue(another);
                if (objValue == null && anotherValue == null)
                    continue;

                if (objValue.GetType() == typeof(List<KeyTimeModel>) && anotherValue.GetType() == typeof(List<KeyTimeModel>))
                {
                    foreach (var item in (List<KeyTimeModel>)objValue)
                    {
                        foreach (var anotherItem in (List<KeyTimeModel>)anotherValue)
                        {
                            if (item.Time == anotherItem.Time)
                            {
                                if (!item.Key.Equals(anotherItem.Key))
                                    result = false;
                                break;
                            }
                        }
                    }
                }
                else if (!objValue.Equals(anotherValue))
                    result = false;
            }

            return result;
        }


        #endregion
    }
}
