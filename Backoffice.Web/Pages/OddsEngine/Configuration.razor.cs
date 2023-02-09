using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Models;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Web.Components.OddsEngine.Configuration;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class Configuration : IAsyncDisposable, IDisposable
    {
        #region Properties
        [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; }
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }        
        [Inject] private IConfiguration AppConfiguration { get; set; }        
        [Inject] public ToastService? ToastService { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        public List<MarketLookupNavigationModel> SelectedContents;
        public List<BreadcrumbModel> Breadcrumbs { get; set; }        
        public List<MarketLookupNavigationModel> MarketLookupNavigationModelsTemp { get; set; }
        public List<AlertModel> Alerts { get; set; }
        
        public ConfigFilterModel ConfigFilterModel { get; set; }                           

        public FixtureType FixtureType { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public int? AllMarketStatus { get; set; }
        public string AllMarketStatusName { get; set; }                                        
        public bool IsMarketEditing { get; set; }        
        private Guid Guid { get; set; }

        #endregion

        #region Fields
        private HubConnection _hubConnection;
        private bool _isConnected => _hubConnection.State == HubConnectionState.Connected;
        private ToastService _toasterService => ToastService!;
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {            
            PageTitle = "Config";
            IconClass = "";            
            ConfigFilterModel = null;
            IsMarketEditing = false;
            AllMarketStatusName = "Deactivate All";
            Guid = new Guid();

            MarketLookupNavigationModelsTemp = new List<MarketLookupNavigationModel>();
            Alerts = new List<AlertModel>();

            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = "odds-engine/config",
                    IsCurrentPage = true
                }
            };            
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try 
            {
                if (firstRender)
                {
                    await Subscribe();
                }
            }
            catch(Exception ex) 
            {

            }            
        }        

        public async Task OnMarketChangedEvent(MarketLookupNavigationModel market)
        {           
            if (!MarketLookupNavigationModelsTemp.Contains(market))
            {
                MarketLookupNavigationModelsTemp.Add(market);
            }
            else
            {
                MarketLookupNavigationModelsTemp.Remove(market);
            }


            if (MarketLookupNavigationModelsTemp.Any())
            {
                IsMarketEditing = true;
            }
            else
            {
                IsMarketEditing = false;
            }            
        }

        public async Task OnMarketEditingCancelClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");            
            parameters.Add("MarketChangedCount", MarketLookupNavigationModelsTemp.Count);
            parameters.Add("OnCancelCallback", EventCallback.Factory.Create(this, UndoEditedMarkets));

            var options = new ModalOptions();
            ModalService.Open<UpdateActiveMarketModal>("Clear changes?", parameters, options);
        }

        public async Task OnMarketSaveClickEvent()
        {
            await OnSaveAllSettings();
        }

        public async Task UndoEditedMarkets()
        {            
            MarketLookupNavigationModelsTemp = new List<MarketLookupNavigationModel>();
            IsMarketEditing = false;            
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
        }
        public void Dispose()
        {
            SelectedContents = null;
            Breadcrumbs = null;
            MarketLookupNavigationModelsTemp = null;
            Alerts = null;
            ConfigFilterModel = null;
            TradingService.Dispose();

            GC.SuppressFinalize(this);
        }
        #endregion

        #region Utilities
        public List<MarketLookupNavigationModel> GetContents()
        {
            if (ConfigFilterModel.MarketTemplateGroupId == 0)
                ConfigFilterModel.MarketTemplateGroupId = ConfigFilterModel.MarketTemplates.FirstOrDefault(m => m.IsSelected).MarketTemplateGroupId;

            var query = ConfigFilterModel.MarketTemplates.Where(c => c.MarketTemplateGroupId == ConfigFilterModel.MarketTemplateGroupId);

            if (!string.IsNullOrEmpty(ConfigFilterModel.SearchText))
            {
                query = query.Where(c => c.MarketLookup.MarketTemplateName.ToUpper().Contains(ConfigFilterModel.SearchText.ToUpper()));
            }

            return query.ToList();
        }

        private void GetNewSettings(MarketLookupNavigationModel market)
        {
            var isActive = false;
            if (ConfigFilterModel.ClientConfig != null)
            {
                if (ConfigFilterModel.ClientConfig.ClientMarket.Any(c => c.ClientCode == ConfigFilterModel.ClientCode
                                                                        && c.SportId == ConfigFilterModel.SportId
                                                                        && c.SegmentId == ConfigFilterModel.SegmentId
                                                                        && c.LeagueId == ConfigFilterModel.LeagueId
                                                                        && c.MarketTemplateId == market.MarketLookup.MarketTemplateId))
                {
                    isActive = ConfigFilterModel.ClientConfig.ClientMarket.FirstOrDefault(c => c.ClientCode == ConfigFilterModel.ClientCode
                                                                        && c.SportId == ConfigFilterModel.SportId
                                                                        && c.SegmentId == ConfigFilterModel.SegmentId
                                                                        && c.LeagueId == ConfigFilterModel.LeagueId
                                                                        && c.MarketTemplateId == market.MarketLookup.MarketTemplateId).IsActive;
                }                    
            }
            if (!MarketLookupNavigationModelsTemp.Contains(market))
            {
                market.MarketLookup.IsActive = isActive;
            }

            if (market.NewMarketLookup == null)
                market.NewMarketLookup = new MarketLookupModel()
                {
                    MarketTemplateId = market.MarketLookup.MarketTemplateId,
                    IsActive = market.MarketLookup.IsActive,
                    MarketTemplateName = market.MarketLookup.MarketTemplateName
                };

            market.PrematchSettings = GetPrematchSettings(market.MarketLookup.MarketTemplateId);
            market.LiveSettings = GetLiveSettings(market.MarketLookup.MarketTemplateId);

            if (market.NewPrematchSettings == null)
                market.NewPrematchSettings = GetPrematchSettings(market.MarketLookup.MarketTemplateId);
            if (market.NewLiveSettings == null)
                market.NewLiveSettings = GetLiveSettings(market.MarketLookup.MarketTemplateId);

        }

        private PrematchSettingsModel GetPrematchSettings(string marketTemplateId)
        {
            var clientCode = ConfigFilterModel.ClientCode;

            var config = new ClientMarketConfig();
            if (ConfigFilterModel.ClientConfig == null)
                config = null;
            else
                config = ConfigFilterModel.ClientConfig.ClientMarket.FirstOrDefault(c => c.ClientCode == clientCode
                && c.SportId == ConfigFilterModel.SportId
                && c.SegmentId == ConfigFilterModel.SegmentId
                && c.LeagueId == ConfigFilterModel.LeagueId
                && c.MarketTemplateId == marketTemplateId);

            if (config == null)
            {
                config = new ClientMarketConfig
                {
                    ClientCode = clientCode,
                    SportId = ConfigFilterModel.SportId,
                    LeagueId = ConfigFilterModel.LeagueId,
                    SegmentId = ConfigFilterModel.SegmentId,
                    MarketTemplateId = marketTemplateId,
                    Live = string.Empty,
                    Prematch = string.Empty
                };
            }

            return new PrematchSettingsModel()
            {
                ClientCode = clientCode,
                SportId = config.SportId,
                LeagueId = config.LeagueId,
                SegmentId = config.SegmentId,
                FirstKey = GetFirstKey(config, true),
                LastKey = GetLastKey(config, true),
                MarketTemplateId = config.MarketTemplateId.ToString(),
                Settings = MockData.PrematchSettings.Select(s => new Backoffice.Shared.Models.KeyTimeModel()
                {
                    Key = GetPrematchKey(s.TimeDisplay, config),
                    Time = s.Time,
                    SortOrder = s.SortOrder,
                    TimeDisplay = s.TimeDisplay
                }).ToList()
            };
        }

        public LiveSettingsModel GetLiveSettings(string marketTemplateId)
        {
            var clientCode = ConfigFilterModel.ClientCode;

            var config = new ClientMarketConfig();
            if (ConfigFilterModel.ClientConfig == null)
                config = null;
            else
                config = ConfigFilterModel.ClientConfig.ClientMarket.FirstOrDefault(c => c.ClientCode == clientCode
                && c.SportId == ConfigFilterModel.SportId
                && c.SegmentId == ConfigFilterModel.SegmentId
                && c.LeagueId == ConfigFilterModel.LeagueId
                && c.MarketTemplateId == marketTemplateId);

            if (config == null)
            {
                config = new ClientMarketConfig
                {
                    ClientCode = clientCode,
                    SportId = ConfigFilterModel.SportId,
                    LeagueId = ConfigFilterModel.LeagueId,
                    SegmentId = ConfigFilterModel.SegmentId,
                    MarketTemplateId = marketTemplateId,
                    Live = string.Empty,
                    Prematch = string.Empty
                };
            }

            var liveSettings = MockData.SportsLiveTimeline.Where(s => s.SportId == config.SportId).FirstOrDefault();

            List<KeyTimeModel> keyTime = new List<KeyTimeModel>();

            if (liveSettings != null)
            {
                foreach (var time in liveSettings.Time)
                {
                    keyTime.Add(new KeyTimeModel { Key = GetLiveKey(time.ToString(), config), TimeDisplay = time.ToString(), Time = time });
                }
            }
            else
            {
                keyTime.Add(new KeyTimeModel { Key = GetLiveKey("0", config), TimeDisplay = "0" });
            }

            return new LiveSettingsModel()
            {
                ClientCode = clientCode,
                SportId = config.SportId,
                LeagueId = config.LeagueId,
                SegmentId = config.SegmentId,
                FirstKey = GetFirstKey(config, false),
                LastKey = GetLastKey(config, false),
                MarketTemplateId = config.MarketTemplateId.ToString(),
                Settings = keyTime,
                OvertimeKey = liveSettings?.Overtime.ToString(),
                LastTimeKey = liveSettings?.Time.Max().ToString()
            };
        }

        public async Task OnAllMarketStatusClickEvent()
        {            
            MarketLookupNavigationModelsTemp = new List<MarketLookupNavigationModel>();

            SelectedContents = GetContents();
            SelectedContents.ForEach(market => GetNewSettings(market));

            if (AllMarketStatusName == "Activate All")
            {
                var allDisabled = SelectedContents.Where(market => market.MarketLookup.IsActive == false).ToList();

                MarketLookupNavigationModelsTemp.AddRange(allDisabled);

                SelectedContents.ForEach(market => market.MarketLookup.IsActive = true);
            }
            else
            {
                var allEnabled = SelectedContents.Where(market => market.MarketLookup.IsActive == true).ToList();

                MarketLookupNavigationModelsTemp.AddRange(allEnabled);

                SelectedContents.ForEach(market => market.MarketLookup.IsActive = false);
            }

            IsMarketEditing = true;

            AllMarketStatusName = AllMarketStatusName == "Deactivate All" ? "Activate All" : "Deactivate All";
            AllMarketStatus = AllMarketStatusName == "Deactivate All" ? 1 : 0;                  
        }

        public async Task OnSaveAllSettings()
        {
            if (SelectedContents != null && SelectedContents.Any())
            {
                ConfigSettingsModel data = new ConfigSettingsModel();
                List<MarketConfig> marketConfig = new List<MarketConfig>();

                data.ClientCode = ConfigFilterModel.ClientCode;
                data.SportId = ConfigFilterModel.SportId;
                data.SegmentId = ConfigFilterModel.SegmentId;
                data.LeagueId = ConfigFilterModel.LeagueId;
                data.ClientId = Guid.Parse(ConfigFilterModel.ClientId);

                var isLeagueUpdate = false;

                foreach (var market in SelectedContents)
                {

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

                    if (!DeepCompare(market.NewPrematchSettings, market.PrematchSettings)
                        || !DeepCompare(market.NewLiveSettings, market.LiveSettings)
                        || !DeepCompare(market.NewMarketLookup, market.MarketLookup))
                    {
                        var prematchFirstKey = prematchSettings.Where(s => s.M >= 101).FirstOrDefault()?.M;
                        var prematchLastKey = prematchSettings.Where(s => s.M > 101).LastOrDefault()?.M;
                        var liveFirstKey = livematchSettings.Where(s => s.M >= 101).FirstOrDefault()?.M;
                        var liveLastKey = livematchSettings.Where(s => s.M > 101).LastOrDefault()?.M;

                        market.NewPrematchSettings.FirstKey = prematchFirstKey == null ? "101" : prematchFirstKey.ToString();
                        market.NewLiveSettings.FirstKey = liveFirstKey == null ? "101" : liveFirstKey.ToString();
                        market.NewPrematchSettings.LastKey = prematchLastKey == null ? "" : prematchLastKey.ToString();
                        market.NewLiveSettings.LastKey = liveFirstKey == null ? "" : liveLastKey.ToString();

                        data.MarketConfig.Add(new MarketConfigModel()
                        {
                            MarketTemplateId = market.MarketLookup.MarketTemplateId,
                            LivematchSettings = livematchSettings,
                            PrematchSettings = prematchSettings,
                            IsActive = market.NewMarketLookup.IsActive
                        });

                        marketConfig.Add(new MarketConfig()
                        {
                            MarketTemplateId = market.MarketLookup.MarketTemplateId,
                            Live = JsonSerializer.Serialize(livematchSettings),
                            Prematch = JsonSerializer.Serialize(prematchSettings),
                            IsActive = market.NewMarketLookup.IsActive,
                            FixtureTypeId = (int)FixtureType
                        });
                    }


                    if (!DeepCompare(market.NewMarketLookup, market.MarketLookup))
                    {
                        isLeagueUpdate = true;
                    }

                }

                var isLeagueActive = false;
                if (isLeagueUpdate)
                {
                    isLeagueActive = SelectedContents.Any(c => c.NewMarketLookup.IsActive) ? true : false;
                    if (!isLeagueActive)
                    {
                        var selectedMarketTemplateIds = SelectedContents.Select(s => s.MarketLookup.MarketTemplateId).ToList();
                        if (selectedMarketTemplateIds != null)
                        {
                            foreach (var unselected in ConfigFilterModel.MarketTemplates)
                                if (!selectedMarketTemplateIds.Contains(unselected.MarketLookup.MarketTemplateId) && unselected.MarketLookup.IsActive == true)
                                    isLeagueActive = true;
                        }
                    }

                    data.LeagueConfig.Add(new LeagueConfigModel()
                    {
                        LeagueId = ConfigFilterModel.LeagueId,
                        SegmentId = ConfigFilterModel.SegmentId,
                        IsActive = SelectedContents.Any(c => c.NewMarketLookup.IsActive) ? true : false
                    });

                }

                data.UserId = UserModel.Id;
                var result = await TradingService.AddOrUpdateConfig(data);
                if (result.StatusCode == StatusCode.Success)
                {
                    await ShowMarketChangedAlert(AlertStatus.Success, "Config has been updated");

                    OnMarketConfigMessage messageMarket = new OnMarketConfigMessage
                    {
                        ClientCode = data.ClientCode,
                        ClientId = data.ClientId,
                        SportId = data.SportId,
                        SegmentId = data.SegmentId,
                        LeagueId = data.LeagueId,
                        Markets = marketConfig
                    };

                    await TradingService.Publish(new MessageRequest()
                    {
                        CorrelationId = Guid.NewGuid().ToString(),
                        MessageType = "OnMarketConfig",
                        RoutingKey = $"r.uof.onmarketconfig",
                        Payload = JsonSerializer.Serialize(messageMarket)
                    });

                    if (isLeagueUpdate)
                    {
                        OnLeagueConfigMessage messageLeague = new OnLeagueConfigMessage
                        {
                            ClientCode = data.ClientCode,
                            ClientId = data.ClientId,
                            SportId = data.SportId,
                            SegmentId = data.SegmentId,
                            LeagueId = data.LeagueId,
                            IsActive = isLeagueActive
                        };

                        await TradingService.Publish(new MessageRequest()
                        {
                            CorrelationId = Guid.NewGuid().ToString(),
                            MessageType = "OnLeagueConfig",
                            RoutingKey = $"r.uof.onleagueconfig",
                            Payload = JsonSerializer.Serialize(messageLeague)
                        });
                    }

                    ConfigFilterModel.ClientConfig = (await TradingService.GetClientConfig(new ClientConfigRequest
                    {
                        ClientCode = ConfigFilterModel.ClientCode,
                        FixtureTypeId = (int)FixtureType,
                        SportId = ConfigFilterModel.SportId,
                        SegmentId = ConfigFilterModel.SegmentId,
                        LeagueId = ConfigFilterModel.LeagueId,
                        IsActive = -99
                    })).Result;

                    if (_isConnected) await _hubConnection.SendAsync("OnConfigChange", data);                    
                }
                else
                {
                    await ShowMarketChangedAlert(AlertStatus.Error, result.Message);
                }

            }

            IsMarketEditing = false;
        }

        public async Task OnModalClear()
        {
            if (ConfigFilterModel.MarketTemplates != null)
            {
                foreach (var market in ConfigFilterModel.MarketTemplates)
                {
                    market.NewMarketLookup = new MarketLookupModel()
                    {
                        MarketTemplateId = market.MarketLookup.MarketTemplateId,
                        IsActive = market.MarketLookup.IsActive,
                        MarketTemplateName = market.MarketLookup.MarketTemplateName
                    };
                    market.NewPrematchSettings = GetPrematchSettings(market.MarketLookup.MarketTemplateId);
                    market.NewLiveSettings = GetLiveSettings(market.MarketLookup.MarketTemplateId);
                }
            }
        }

        public async ValueTask<ItemsProviderResult<MarketLookupNavigationModel>> GetItemProviderContents(ItemsProviderRequest request)
        {
            var contents = SelectedContents.Skip(request.StartIndex).Take(request.Count);

             return new ItemsProviderResult<MarketLookupNavigationModel>(contents, SelectedContents.Count());
        }

        private string GetFirstKey(ClientMarketConfig config, bool isPrematch)
        {
            //for testing only
            if (config == null || (string.IsNullOrEmpty(config.Prematch) && string.IsNullOrEmpty(config.Live)))
            {
                if (ConfigFilterModel.ClientCode == "10C" || ConfigFilterModel.ClientCode == "10I")
                    return "112";
                if (ConfigFilterModel.ClientCode == "10J")
                    return "109";
            }
            //end

            if (config == null) return "101";
            if ((isPrematch && config.Prematch?.Length == 0)
                || (!isPrematch && config.Live?.Length == 0)) return "101";

            List<MarketMargin> keyTimes = JsonSerializer.Deserialize<List<MarketMargin>>(isPrematch ? config.Prematch : config.Live);

            if (keyTimes == null) return "";

            var firstKey = keyTimes.Where(s => s.M >= 101).FirstOrDefault()?.M;
            return (firstKey ?? 101).ToString();
        }

        private string GetLastKey(ClientMarketConfig config, bool isPrematch)
        {
            if (config == null) return "";
            if ((isPrematch && config.Prematch?.Length == 0)
                || (!isPrematch && config.Live?.Length == 0)) return "";

            List<MarketMargin> keyTimes = JsonSerializer.Deserialize<List<MarketMargin>>(isPrematch ? config.Prematch : config.Live);
            if (keyTimes == null) return "";

            var lastKey = keyTimes.Where(s => s.M > 101).LastOrDefault()?.M;
            return lastKey == null ? "" : lastKey.ToString();
        }

        private int? GetPrematchKey(string Time, ClientMarketConfig config)
        {
            //for testing only
            if (config == null || string.IsNullOrEmpty(config.Prematch))
            {
                if (ConfigFilterModel.ClientCode == "10C" || ConfigFilterModel.ClientCode == "10I")
                    if (Time == "INST")
                        return 112;
                if (ConfigFilterModel.ClientCode == "10J")
                    if (Time == "INST")
                        return 109;
            }
            //end

            if (config == null)
            {
                if (Time == "INST")
                    return 101;
                else
                    return null;
            }

            if (string.IsNullOrEmpty(config.Prematch))
            {
                if (Time == "INST")
                    return 101;
                else
                    return null;
            }


            List<MarketMargin> keyTimes = JsonSerializer.Deserialize<List<MarketMargin>>(config.Prematch);
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

        private int? GetLiveKey(string Time, ClientMarketConfig config)
        {
            //for testing only
            if (config == null || string.IsNullOrEmpty(config.Live))
            {
                if (ConfigFilterModel.ClientCode == "10C" || ConfigFilterModel.ClientCode == "10I")
                    if (Time == "0")
                        return 112;
                if (ConfigFilterModel.ClientCode == "10J")
                    if (Time == "0")
                        return 109;
            }
            //end

            if (config == null)
            {
                if (Time == "0")
                    return 101;
                else
                    return null;
            }

            if (string.IsNullOrEmpty(config.Live))
            {
                if (Time == "0")
                    return 101;
                else
                    return null;
            }

            List<MarketMargin> keyTimes = JsonSerializer.Deserialize<List<MarketMargin>>(config.Live);
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

        private async Task Subscribe()
        {
            var request = HttpContextAccessor.HttpContext?.Request;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(new Uri(AppConfiguration["API:HubAPI"]))
                .Build();

            _hubConnection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await _hubConnection.StartAsync();
            };
            _hubConnection.On<ConfigSettingsModel>("onconfigchange", async message =>
            {
                await ProcessOnConfigChange(message);
            });

            _hubConnection.On<NotificationsModel>("onnewnotification", async message =>
            {
                await ProcessNotification(message);
            });


            await _hubConnection.StartAsync();            
        }

        private async Task ProcessOnConfigChange(ConfigSettingsModel message)
        {
            if (ConfigFilterModel.ClientCode == message.ClientCode)
            {
                var isUpdated = false;

                var clientConfig = ConfigFilterModel.ClientConfig;
                foreach (var marketSetting in message.MarketConfig)
                {
                    isUpdated = false;
                    //update existing config
                    foreach (var marketConfig in clientConfig.ClientMarket)
                    {
                        if (marketConfig.MarketTemplateId == marketSetting.MarketTemplateId)
                        {
                            marketConfig.IsActive = marketSetting.IsActive;
                            marketConfig.Prematch = marketSetting.PrematchSettings == null ? marketConfig.Prematch : JsonSerializer.Serialize(marketSetting.PrematchSettings);
                            marketConfig.Live = marketSetting.LivematchSettings == null ? marketConfig.Live : JsonSerializer.Serialize(marketSetting.LivematchSettings);
                            isUpdated = true;
                            break;
                        }
                    }
                    //add new config
                    if (!isUpdated)
                    {
                        clientConfig.ClientMarket.Add(new ClientMarketConfig()
                        {
                            SportId = message.SportId,
                            ClientCode = message.ClientCode,
                            FixtureTypeId = (int)FixtureType,
                            MarketTemplateId = marketSetting.MarketTemplateId,
                            IsActive = marketSetting.IsActive,
                            Prematch = JsonSerializer.Serialize(marketSetting.PrematchSettings),
                            Live = JsonSerializer.Serialize(marketSetting.LivematchSettings)
                        });
                    }

                }

                StateHasChanged();
            }            
        }
        private async Task ProcessNotification(NotificationsModel message) 
        {
            if (message != null)
            {
                if (message.Page.Contains("Config"))
                {
                    _toasterService.AddToast(Toast.NewToast("Export Margins", message.Details, true));
                }
            }
        }

        public async Task ShowMarketChangedAlert(AlertStatus status, string message)
        {
            Alerts.Clear();

            Alerts.Add(new AlertModel()
            {
                Status = status.ToString(),
                Message = message,
                Opacity = 1.0m
            });

            Guid = Guid.NewGuid();

            StateHasChanged();
        }

        public async Task OnCopyClientSettingsAlert()
        {
            Alerts.Clear();

            Alerts.Add(new AlertModel()
            {
                Status = AlertStatus.Success.ToString(),
                Message = "Success! Copy Client Settings is being generated. System will notify once done.",
                Opacity = 1.0m
            });

            Guid = Guid.NewGuid();
        }

        #endregion
    }
}
