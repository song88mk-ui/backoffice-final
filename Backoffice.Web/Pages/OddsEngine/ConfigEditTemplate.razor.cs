using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models;
using Backoffice.Web.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using Activity = Backoffice.Shared.Enums.Activity;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ConfigEditTemplate : IDisposable
    {
        #region Properties
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [Parameter] public string Region { get; set; } = "hk";
        [Parameter] public long ConfigTemplateId { get; set; }
        [Parameter] public string Action { get; set; }
        [Parameter] public bool View { get; set; }

        private List<BreadcrumbModel> _breadcrumbs;
        private List<MarketLookupNavigationModel> _marketLookupNavigation;


        private ConfigTemplateMarginModel _configTemplateMargin;
        private EditConfigTemplateFilterModel _editConfigTemplateFilterModel;
        private int _selectedClientCount;
        private string _selectedClientName;
        private int _selectedLeagueCount;
        private string _selectedLeagueName;
        private string _sportIcon;
        private string _configTemplateName;
        private string _sportName;
        private string _scope;
        private string _allMarketStatusName = "ACTIVATE ALL";
        private bool _isPreloader = true;
        private bool _hasChanges;
        private bool _isNameChanges;
        private AlertModel _alertModel;
        private Activity? _activity;
        private bool _isExisitngName;
        private bool _isProcessing;
        private bool _isEdttingName;
        private string _temporaryName;

        #endregion


        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;
            await PageState.AuthenticateAccessEvent.InvokeAsync();

            _isPreloader = true;

            _configTemplateMargin = new ConfigTemplateMarginModel();

            _marketLookupNavigation = new List<MarketLookupNavigationModel>();
            _breadcrumbs = new List<BreadcrumbModel>()
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

            _hasChanges = _activity == Activity.Copy;

            await LoadConfigTemplateMargin();
        }
        public async Task InitConfigEditTemplate(EditConfigTemplateFilterModel model)
        {
            _isPreloader = true;

            _editConfigTemplateFilterModel = model;

            _sportIcon = Utility.GetSportIcon(_editConfigTemplateFilterModel.SportId);
            _sportName = _editConfigTemplateFilterModel.SportName;
            _configTemplateName = $"{(_activity == Activity.Copy ? "Copy of " : "")}{_editConfigTemplateFilterModel.ConfigTemplate.Name}";

            _scope = ((FixtureStatus)_editConfigTemplateFilterModel.ScopeId).ToString();

            _breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = $"hk/odds-engine/config-templates",
                    IsCurrentPage = false
                },
                new BreadcrumbModel
                {
                    Name = $"{_configTemplateName}",
                    Url = $"hk/odds-engine/config-templates/edit/{ConfigTemplateId}",
                    IsCurrentPage = true
                }
            };

            await LoadMarkets();

            _allMarketStatusName = _marketLookupNavigation.Any(m => m.IsVisible && m.NewMarketLookup.IsActive == false) ? "ACTIVATE ALL" : "DEACTIVATE ALL";

            _isPreloader = false;
        }

        public async Task OnAllMarketStatusClickEvent()
        {

            if (_allMarketStatusName == "ACTIVATE ALL")
            {
                _marketLookupNavigation.Where(m => m.IsVisible).ToList().ForEach(market => market.NewMarketLookup.IsActive = true);
            }
            else
            {
                _marketLookupNavigation.Where(m => m.IsVisible).ToList().ForEach(market => market.NewMarketLookup.IsActive = false);
            }

            _allMarketStatusName = _allMarketStatusName == "DEACTIVATE ALL" ? "ACTIVATE ALL" : "DEACTIVATE ALL";

            await OnTemplateMarginChange();
        }

        public async Task OnTemplateMarginChange()
        {
            if (_activity == Activity.Copy)
            {
                _hasChanges = true;
            }
            else
            {
                foreach (var market in _marketLookupNavigation)
                {
                    _hasChanges = !DeepCompare(market.MarketLookup, market.NewMarketLookup);
                    if (_hasChanges)
                        break;

                    _hasChanges = !DeepCompare(market.PrematchSettings, market.NewPrematchSettings);
                    if (_hasChanges)
                        break;

                    _hasChanges = !DeepCompare(market.LiveSettings, market.NewLiveSettings);
                    if (_hasChanges)
                        break;

                }
            }
        }

        public async Task OnSaveMarginTemplate()
        {
            if (_isProcessing)
                return;

            _isPreloader = true;
            _isProcessing = true;
            var isNavigate = false;

            try
            {
                var referenceConfigTemplateId = ConfigTemplateId;

                if (_isNameChanges || _activity == Activity.Copy)
                {
                    var payload = new ConfigTemplateModel()
                    {
                        SportId = _editConfigTemplateFilterModel.SportId,
                        FixtureTypeId = _editConfigTemplateFilterModel.ConfigTemplate.FixtureTypeId,
                        FixtureStatusId = _editConfigTemplateFilterModel.ScopeId,
                        Name = _configTemplateName,
                    };

                    if (_isNameChanges)
                    {
                        if (string.IsNullOrEmpty(_configTemplateName))
                        {
                            _isExisitngName = true;
                            return;
                        }

                        payload.Id = referenceConfigTemplateId;
                    }

                    var response = await _backofficeService.AddOrUpdateConfigTemplate(payload);
                    if (response.StatusCode == StatusCode.Success)
                    {
                        payload.Id = response.Result;
                        var oldConfigTemplate = new ConfigTemplateModel()
                        {
                            Id = _editConfigTemplateFilterModel.ConfigTemplate.Id,
                            SportId = _editConfigTemplateFilterModel.SportId,
                            FixtureTypeId = _editConfigTemplateFilterModel.ConfigTemplate.FixtureTypeId,
                            FixtureStatusId = _editConfigTemplateFilterModel.ScopeId,
                            Name = _editConfigTemplateFilterModel.ConfigTemplate.Name
                        };

                        var activityLogPayload = new ActivityLogModel()
                        {
                            Page = SportsbookClientPermission.SBC_OE_Config.ToString().Replace("_", "."),
                            From = JsonSerializer.Serialize(oldConfigTemplate),
                            To = JsonSerializer.Serialize(payload),
                            UserId = UserModel.Id,
                            UserName = UserModel.UserName,
                            TimeStamp = DateTime.UtcNow
                        };

                        if (_activity == Activity.Copy)
                        {
                            activityLogPayload.Payload = $"{_editConfigTemplateFilterModel.SportId}.{_editConfigTemplateFilterModel.ScopeId}.{payload.Name}.T".Replace(" ", "_");
                            activityLogPayload.Activity = Activity.Copy.ToString();

                            await _backofficeService.AddActivityLog(activityLogPayload);
                        }
                        else if (_isNameChanges)
                        {
                            activityLogPayload.Payload = $"{_editConfigTemplateFilterModel.SportId}.{_editConfigTemplateFilterModel.ScopeId}.{_editConfigTemplateFilterModel.ConfigTemplate.Name}.N".Replace(" ", "_");
                            activityLogPayload.Activity = Activity.Update.ToString();

                            await _backofficeService.AddActivityLog(activityLogPayload);
                        }

                        referenceConfigTemplateId = response.Result;
                    }
                    else
                    {
                        _isExisitngName = true;
                        return;
                    }

                    _isNameChanges = false;
                }

                if (_hasChanges)
                {
                    ConfigTemplateMarginModel request = new ConfigTemplateMarginModel();

                    var activityLog = new List<ActivityLogModel>();
                    var oldConfigMarginsLogs = new List<ConfigMarginActivityLogModel>();
                    var newConfigMarginsLogs = new List<ConfigMarginActivityLogModel>();
                    var oldMarketState = new List<ConfigMarginActivityLogModel>();
                    var newMarketState = new List<ConfigMarginActivityLogModel>();

                    var marginHasChange = false;
                    var statusHasChange = false;

                    foreach (var market in _marketLookupNavigation)
                    {
                        //validate Odds
                        if (market.NewMarketLookup.IsActive)
                        {
                            switch ((FixtureStatus)_editConfigTemplateFilterModel.ScopeId)
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

                        switch ((FixtureStatus)_editConfigTemplateFilterModel.ScopeId)
                        {
                            case FixtureStatus.Prematch:
                                if (market.NewPrematchSettings.Settings.Any(x => market.PrematchSettings.Settings.Any(f => f.Time == x.Time && f.Key != x.Key)))
                                {
                                    if (_activity != Activity.Copy)
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

                                    marginHasChange = true;
                                }
                                break;
                            case FixtureStatus.Live:
                                if (market.NewLiveSettings.Settings.Any(x => market.LiveSettings.Settings.Any(f => f.Time == x.Time && f.Key != x.Key)))
                                {
                                    if (_activity != Activity.Copy)
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

                                    marginHasChange = true;
                                }
                                break;
                        }

                        if (market.MarketLookup.IsActive != market.NewMarketLookup.IsActive)
                        {
                            if (_activity != Activity.Copy)
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

                            statusHasChange = true;
                        }

                        request.ConfigTemplateMargins.Add(new ConfigTemplateMargin
                        {
                            ConfigTemplateId = referenceConfigTemplateId,
                            SportId = _editConfigTemplateFilterModel.SportId,
                            MarketTemplateId = market.MarketLookup.MarketTemplateId,
                            FixtureTypeId = (int)FixtureType.Match,
                            FixtureStatusId = _editConfigTemplateFilterModel.ScopeId,
                            IsActive = market.NewMarketLookup.IsActive,
                            Config = _editConfigTemplateFilterModel.ScopeId == (int)FixtureStatus.Prematch ? JsonSerializer.Serialize(prematchSettings) : JsonSerializer.Serialize(livematchSettings)
                        });
                    }

                    var result = await _backofficeService.AddOrUpdateConfigTemplateMargin(request);

                    if (result.StatusCode == StatusCode.Success)
                    {
                        if (marginHasChange)
                            await _backofficeService.AddActivityLog(new ActivityLogModel()
                            {
                                Page = SportsbookClientPermission.SBC_OE_Config.ToString().Replace("_", "."),
                                Payload = $"{_editConfigTemplateFilterModel.SportId}.{_editConfigTemplateFilterModel.ScopeId}.{_configTemplateName}.Modified_Margins.M".Replace(" ", "_"),
                                From = _activity == Activity.Copy ? "-" : JsonSerializer.Serialize(oldConfigMarginsLogs),
                                To = JsonSerializer.Serialize(newConfigMarginsLogs),
                                Activity = Activity.Update.ToString(),
                                UserId = UserModel.Id,
                                UserName = UserModel.UserName,
                                TimeStamp = DateTime.UtcNow
                            });

                        if (statusHasChange)
                            await _backofficeService.AddActivityLog(new ActivityLogModel()
                            {
                                Page = SportsbookClientPermission.SBC_OE_Config.ToString().Replace("_", "."),
                                Payload = $"{_editConfigTemplateFilterModel.SportId}.{_editConfigTemplateFilterModel.ScopeId}.{_configTemplateName}.Changed_Market_State.M".Replace(" ", "_"),
                                From = _activity == Activity.Copy ? "-" : JsonSerializer.Serialize(oldMarketState),
                                To = JsonSerializer.Serialize(newMarketState),
                                Activity = Activity.Update.ToString(),
                                UserId = UserModel.Id,
                                UserName = UserModel.UserName,
                                TimeStamp = DateTime.UtcNow
                            });

                        isNavigate = true;
                    }
                }

                if (isNavigate)
                    NavigationManager.NavigateTo($"hk/odds-engine/config-templates?message=Successfully updated");
            }
            catch (Exception ex)
            { }
            finally
            {
                _isPreloader = false;
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
            _temporaryName = _configTemplateName.Trim();
        }
        public async Task OnSaveTemplateNameClicked()
        {
            _isEdttingName = false;
            _isExisitngName = false;

            if (string.IsNullOrEmpty(_temporaryName))
                return;

            _configTemplateName = _temporaryName.Trim();
            _isExisitngName = false;

            if (_configTemplateName != _editConfigTemplateFilterModel.ConfigTemplate.Name)
            {
                if (_activity != Activity.Copy)
                    _isNameChanges = true;

                _breadcrumbs.Last().Name = _configTemplateName;
            }
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
            var response = await _backofficeService.GetConfigTemplateMargin(ConfigTemplateId);

            if (response.StatusCode == StatusCode.Success)
                _configTemplateMargin = response.Result;
        }


        private async Task LoadMarkets()
        {
            if (_editConfigTemplateFilterModel.MarketTemplateGroups.Count > 0)
            {
                await GetMarketTemplate(_editConfigTemplateFilterModel.MarketTemplateGroups);
            }
        }


        private async Task GetMarketTemplate(List<MarketGroupNavigationModel> MarketTemplateGroups)
        {

            foreach (var MarketTemplateGroup in MarketTemplateGroups)
            {
                //if (!EditConfigTemplateFilterModel.SelectedMarkets.Contains(MarketTemplateGroup.MarketTemplateGroup.MarketTemplateGroupId.ToString()))
                //    continue;

                var marketTemplateGroup = MarketTemplateGroup.MarketTemplateGroup;
                foreach (var marketLookup in _editConfigTemplateFilterModel.MarketLookup.MarketLookup)
                {
                    if (marketTemplateGroup.MarketTemplateIds.Contains(marketLookup.MarketTemplateId))
                    {
                        //if (!(string.IsNullOrEmpty(EditConfigTemplateFilterModel.SearchText) || marketLookup.MarketTemplateName.Contains(EditConfigTemplateFilterModel.SearchText, StringComparison.OrdinalIgnoreCase)))
                        //    continue;

                        var isVisible = (((_editConfigTemplateFilterModel.SelectedMarkets.Any() || MarketTemplateGroups.Count == 1) && MarketTemplateGroup.MarketTemplateGroup.MarketTemplateGroupId == 1)
                                || _editConfigTemplateFilterModel.SelectedMarkets.Contains(MarketTemplateGroup.MarketTemplateGroup.MarketTemplateGroupId.ToString()))
                                                && (string.IsNullOrEmpty(_editConfigTemplateFilterModel.SearchText) || marketLookup.MarketTemplateName.Contains(_editConfigTemplateFilterModel.SearchText, StringComparison.OrdinalIgnoreCase))
                                                && _editConfigTemplateFilterModel.SelectedConfigStatus.Contains(marketLookup.IsActive ? "1" : "2");

                        var newMarketTemplate = _marketLookupNavigation.FirstOrDefault(m => m.MarketLookup.MarketTemplateId == marketLookup.MarketTemplateId);
                        if (newMarketTemplate == null)
                        {

                            var configTemplateMargin = _configTemplateMargin.ConfigTemplateMargins.FirstOrDefault(c => c.MarketTemplateId == marketLookup.MarketTemplateId);
                            if (configTemplateMargin == null)
                            {
                                configTemplateMargin = new Backoffice.Shared.Models.ConfigTemplateMargin()
                                {
                                    MarketTemplateId = marketLookup.MarketTemplateId,
                                    IsActive = false,
                                    FixtureStatusId = _editConfigTemplateFilterModel.ScopeId,
                                    FixtureTypeId = (int)FixtureType.Match,
                                    SportId = _editConfigTemplateFilterModel.SportId
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


                            _marketLookupNavigation.Add(new MarketLookupNavigationModel()
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

        private PrematchSettingsTemplateModel GetPrematchSettingsTemplate(ConfigTemplateMargin configTemplateMargin, string marketTemplateId, string marketTemplateName) => new PrematchSettingsTemplateModel()
        {
            SportId = _editConfigTemplateFilterModel.ConfigTemplate.SportId,
            FirstKey = GetFirstKey(configTemplateMargin),
            LastKey = GetLastKey(configTemplateMargin),
            MarketTemplateId = marketTemplateId,
            Settings = MockData.PrematchSettings.Select(s => new KeyTimeModel()
            {
                Key = GetPrematchKey(s.TimeDisplay, configTemplateMargin),
                Time = s.Time,
                SortOrder = s.SortOrder,
                TimeDisplay = s.TimeDisplay
            }).ToList()
        };

        private async Task<ConfigTemplateMarginModel> GetConfigTemplateMargin(long configTemplateId)
        {
            var response = await _backofficeService.GetConfigTemplateMargin(configTemplateId);

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
            payload.SportId = _editConfigTemplateFilterModel.ConfigTemplate.SportId;

            var response = await _backofficeService.GetConfigClient(ConfigState.Assigned, payload, configTemplateId);

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

        public LiveSettingsTemplateModel GetLiveSettingsTemplate(ConfigTemplateMargin configTemplateMargin, string marketTemplateId)
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

        private string GetFirstKey(ConfigTemplateMargin configTemplateMargin)
        {

            if (configTemplateMargin == null || string.IsNullOrEmpty(configTemplateMargin.Config))
                return "101";

            if (configTemplateMargin.Config?.Length == 0)
                return "101";

            List<MarketMargin> keyTimes = JsonSerializer.Deserialize<List<MarketMargin>>(configTemplateMargin.Config);

            if (keyTimes == null)
                return "";

            var firstKey = keyTimes.Where(s => s.M >= 101).FirstOrDefault()?.M;
            return (firstKey ?? 101).ToString();
        }

        private string GetLastKey(ConfigTemplateMargin configTemplateMargin)
        {
            if (configTemplateMargin == null || string.IsNullOrEmpty(configTemplateMargin.Config))
                return "101";

            if (configTemplateMargin.Config?.Length == 0)
                return "";

            List<MarketMargin> keyTimes = JsonSerializer.Deserialize<List<MarketMargin>>(configTemplateMargin.Config);
            if (keyTimes == null)
                return "";

            var lastKey = keyTimes.Where(s => s.M > 101).LastOrDefault()?.M;
            return lastKey == null ? "" : lastKey.ToString();
        }

        private int? GetPrematchKey(string Time, ConfigTemplateMargin configTemplateMargin)
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
            if (timeDisplay == null)
                return null;
            var keyTime = keyTimes.Where(a => a.T == timeDisplay.Time).FirstOrDefault();
            if (keyTime == null)
                return null;
            return keyTime.M;
        }

        private int? GetLiveKey(string Time, ConfigTemplateMargin configTemplateMargin)
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
            if (keyTime == null)
                return null;
            return keyTime.M;

        }

        protected bool DeepCompare(object obj, object another)
        {
            if (ReferenceEquals(obj, another))
                return true;
            if ((obj == null) || (another == null))
                return false;
            //Compare two object's class, return false if they are difference
            if (obj.GetType() != another.GetType())
                return false;

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
