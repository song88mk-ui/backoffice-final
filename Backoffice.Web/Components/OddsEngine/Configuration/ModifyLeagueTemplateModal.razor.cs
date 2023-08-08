using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ModifyLeagueTemplateModal : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        [Parameter] public EventCallback<object> OnSaveClickCallback { get; set; }

        public List<ConfigClientViewModel> Leagues { get; set; }
        public List<SelectListItemModel<long>> PrematchTemplates { get; set; }
        public List<SelectListItemModel<long>> LiveTemplates { get; set; }


        public bool IsRemoveCurrentPrematchTemplate { get; set; } = false;
        public bool IsRemoveCurrentLiveTemplate { get; set; } = false;

        private ConfigState State = ConfigState.Assigned;
        private bool IsPreLoader = false;
        private bool IsShowAll = false;
        private string ActivityLogPayload = "";
        private int SportId = 0;
        private long CurrentPrematchId = 0;
        private long CurrentLiveId = 0;
        private bool _hasChanged = false;
        private AlertModel _alertModel;
        private SelectListItemModel<long> _prematchSelectedItem;
        private SelectListItemModel<long> _liveSelectedItem;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            IsPreLoader = true;
            PrematchTemplates = new List<SelectListItemModel<long>>();
            LiveTemplates = new List<SelectListItemModel<long>>();
            Leagues = new List<ConfigClientViewModel>();

            Leagues = ModalParameters.TryGet<List<ConfigClientViewModel>>("Leagues");
            State = ModalParameters.TryGet<ConfigState>("State");
            SportId = ModalParameters.TryGet<int>("SportId");
            CurrentPrematchId = ModalParameters.TryGet<long>("CurrentPrematchId");
            CurrentLiveId = ModalParameters.TryGet<long>("CurrentLiveId");
            ActivityLogPayload = $"{SportId}";
            PrematchTemplates = await GetTemplates(SportId, (int)FixtureStatus.Prematch);
            LiveTemplates = await GetTemplates(SportId, (int)FixtureStatus.Live);

            if (CurrentPrematchId > 0 && Leagues.All(x => x.PrematchTemplateId == CurrentPrematchId))
            {
                var prematchSelectedName = PrematchTemplates.FirstOrDefault(p => p.Value == CurrentPrematchId)?.Name ?? string.Empty;
                _prematchSelectedItem = new SelectListItemModel<long>() { Value = CurrentPrematchId, Name = prematchSelectedName };
            }
            else
            {
                CurrentPrematchId = 0;
            }

            if (CurrentLiveId > 0 && Leagues.All(x => x.LiveTemplateId == CurrentLiveId))
            {
                var liveSelectedName = LiveTemplates.FirstOrDefault(p => p.Value == CurrentLiveId)?.Name ?? string.Empty;
                _liveSelectedItem = new SelectListItemModel<long>() { Value = CurrentLiveId, Name = liveSelectedName };
            }
            else
            {
                CurrentLiveId = 0;
            }

        }

        public async Task OnPrematchTemplateSelectChanged(SelectListItemModel<long> e)
        {
            _prematchSelectedItem = new SelectListItemModel<long>();
            _prematchSelectedItem = e;

            _hasChanged = CurrentPrematchId != _prematchSelectedItem.Value && _liveSelectedItem != null;

            IsRemoveCurrentPrematchTemplate = false;

            if (_prematchSelectedItem != null && _prematchSelectedItem.Value == -1)
            {
                IsRemoveCurrentPrematchTemplate = true;
            }
        }

        public async Task OnLiveTemplateSelectChanged(SelectListItemModel<long> e)
        {
            _liveSelectedItem = new SelectListItemModel<long>();
            _liveSelectedItem = e;

            _hasChanged = CurrentLiveId != _liveSelectedItem.Value && _prematchSelectedItem != null;

            IsRemoveCurrentLiveTemplate = false;

            if (_liveSelectedItem != null && _liveSelectedItem.Value == -1)
            {
                IsRemoveCurrentLiveTemplate = true;
            }
        }

        public async Task OnCancelClickedEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnSaveClickedEvent()
        {
            IsPreLoader = true;
            StateHasChanged();

            if (_prematchSelectedItem != null || _liveSelectedItem != null)
            {
                var leagueRequest = Leagues.Select(x => new ConfigClientModel()
                {
                    Id = x.Id,
                    ClientId = x.ClientId,
                    ClientCode = x.ClientCode,
                    ClientName = x.ClientName,
                    SportId = x.SportId,
                    SportName = x.SportName,
                    SegmentId = x.SegmentId,
                    SegmentName = x.SegmentName,
                    LeagueId = x.LeagueId,
                    LeagueName = x.LeagueName,
                    PrematchTemplateId = x.PrematchTemplateId,
                    PrematchTemplateName = x.PrematchTemplateName,
                    LiveTemplateId = x.LiveTemplateId,
                    LiveTemplateName = x.LiveTemplateName
                }).ToList();

                var state = ConfigState.UnAssigned;
                if (_prematchSelectedItem?.Value > 0 || _liveSelectedItem?.Value > 0)
                {
                    state = ConfigState.Assigned;
                }

                var response = await BackOfficeService.SetLeagueState(state, new ConfigSetLeagueStateRequest()
                {
                    PrematchTemplateId = _prematchSelectedItem?.Value ?? -1,
                    LiveTemplateId = _liveSelectedItem?.Value ?? -1,
                    Leagues = leagueRequest
                });

                if (response.StatusCode == StatusCode.Success)
                {
                    List<MarketConfig> marketConfig = new List<MarketConfig>();

                    var prematchTemplateId = _prematchSelectedItem?.Value > 0 ? _prematchSelectedItem.Value : leagueRequest.FirstOrDefault().PrematchTemplateId;

                    if (prematchTemplateId > 0)
                    {
                        var prematchConfigTemplateMargins = await GetConfigTemplateMargin(prematchTemplateId);
                        foreach (var margin in prematchConfigTemplateMargins.ConfigTemplateMargins)
                        {
                            marketConfig.Add(new MarketConfig()
                            {
                                MarketTemplateId = margin.MarketTemplateId,
                                Live = _liveSelectedItem != null && _liveSelectedItem.Value > 0 ? margin.Config : string.Empty,
                                Prematch = _prematchSelectedItem != null && _prematchSelectedItem.Value > 0 ? margin.Config : string.Empty,
                                IsActive = state == ConfigState.Assigned ? margin.IsActive : false,
                                FixtureTypeId = (int)FixtureType.Match
                            });
                        }
                    }


                    var liveTemplateId = _liveSelectedItem?.Value > 0 ? _liveSelectedItem.Value : leagueRequest.FirstOrDefault().LiveTemplateId;

                    if (liveTemplateId > 0)
                    {
                        var liveConfigTemplateMargins = await GetConfigTemplateMargin(liveTemplateId);

                        foreach (var margin in liveConfigTemplateMargins.ConfigTemplateMargins)
                        {
                            var mConfig = marketConfig.FirstOrDefault(m => m.MarketTemplateId == margin.MarketTemplateId);
                            if (mConfig == null)
                            {
                                marketConfig.Add(new MarketConfig()
                                {
                                    MarketTemplateId = margin.MarketTemplateId,
                                    Live = _liveSelectedItem != null && _liveSelectedItem.Value > 0 ? margin.Config : string.Empty,
                                    Prematch = string.Empty,
                                    IsActive = state == ConfigState.Assigned ? margin.IsActive : false,
                                    FixtureTypeId = (int)FixtureType.Match
                                });
                            }
                            else
                            {
                                mConfig.Live = _liveSelectedItem != null && _liveSelectedItem.Value > 0 ? margin.Config : string.Empty;
                                if (margin.IsActive)
                                    mConfig.IsActive = true;
                            }

                        }
                    }


                    foreach (var league in leagueRequest)
                    {
                        ConfigSettingsModel data = new ConfigSettingsModel();
                        data.ClientCode = league.ClientCode;
                        data.SportId = league.SportId;
                        data.SegmentId = league.SegmentId;
                        data.LeagueId = league.LeagueId;
                        data.ClientId = Guid.Parse(league.ClientId);
                        data.UserId = UserModel.Id;
                        data.UserName = UserModel.UserName;

                        data.MarketConfig = marketConfig.Select(s => new MarketConfigModel
                        {
                            IsActive = s.IsActive,
                            MarketTemplateId = s.MarketTemplateId,
                            PrematchSettings = string.IsNullOrEmpty(s.Prematch) ? new List<MarketMargin>() { new MarketMargin { T = -41472000, M = 0 } } : JsonSerializer.Deserialize<List<MarketMargin>>(s.Prematch)?.ToList() ?? new List<MarketMargin>(),
                            LivematchSettings = string.IsNullOrEmpty(s.Live) ? new List<MarketMargin>() { new MarketMargin { T = 0, M = 0 } } : JsonSerializer.Deserialize<List<MarketMargin>>(s.Live)?.ToList() ?? new List<MarketMargin>(),
                        }).ToList();

                        data.LeagueConfig = new List<LeagueConfigModel>()
                        {
                            new LeagueConfigModel
                            {
                                IsActive = state == ConfigState.Assigned ? data.MarketConfig.Any(m => m.IsActive) : false,
                                LeagueId = league.LeagueId,
                                SegmentId = league.SegmentId
                            }
                        };

                        var result = await TradingService.AddOrUpdateConfig(data);
                        if (result.StatusCode == StatusCode.Success)
                        {

                            var messageLeague = new OnLeagueConfigMessage
                            {
                                ClientCode = league.ClientCode,
                                ClientId = Guid.Parse(league.ClientId),
                                SportId = league.SportId,
                                SegmentId = league.SegmentId,
                                LeagueId = league.LeagueId,
                                IsActive = state == ConfigState.Assigned
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
                                SportId = SportId,
                                SegmentId = league.SegmentId,
                                LeagueId = league.LeagueId,
                                Markets = marketConfig
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

                    var distinctPrematchTemplates = Leagues.Select(x => new
                    {
                        Id = x.PrematchTemplateId.ToString(),
                        Name = x.PrematchTemplateName
                    }).Distinct().ToList();
                    var distinctLiveTemplates = Leagues.Select(x => new
                    {
                        Id = x.LiveTemplateId.ToString(),
                        Name = x.LiveTemplateName
                    }).Distinct().ToList();

                    var cancellationTokenSource = new CancellationTokenSource();

                    Parallel.ForEach(distinctPrematchTemplates, async (item) =>
                    {
                        if (_prematchSelectedItem != null && _prematchSelectedItem.Value.ToString() != item.Id)
                        {
                            var leagues = Leagues.Where(x => x.PrematchTemplateId.ToString() == item.Id).ToList();

                            var from = "";
                            var to = "";
                            from = JsonSerializer.Serialize(new ConfigTemplateLogModel()
                            {
                                TemplateName = item.Id != "-1" ? item.Name : "Unassigned",
                                Payloads = new ConfigTemplateLogPayloadModel()
                                {
                                    Clients = leagues.Select(x => x.ClientName).Distinct().ToList(),
                                    Leagues = leagues.Select(x => x.LeagueName).Distinct().ToList()
                                }
                            });
                            to = JsonSerializer.Serialize(new ConfigTemplateLogModel()
                            {
                                TemplateName = _prematchSelectedItem != null && _prematchSelectedItem.Value.ToString() != "-1" ? _prematchSelectedItem.Name : "Unassigned",
                                Payloads = new ConfigTemplateLogPayloadModel()
                                {
                                    Clients = new List<string>(),
                                    Leagues = new List<string>()
                                }
                            });

                            var activityLog = new ActivityLogModel()
                            {
                                Page = SportsbookClientPermission.SBC_OE_Config.ToString().Replace("_", "."),
                                Payload = $"{ActivityLogPayload}.{(int)FixtureStatus.Prematch}.{(_prematchSelectedItem.Value.ToString() == "-1" ? "Removed Current Template" : _prematchSelectedItem?.Name ?? "")}.T".Replace(" ", "_"),
                                From = from,
                                To = to,
                                Activity = Activity.Update.ToString(),
                                UserId = UserModel.Id,
                                UserName = UserModel.UserName,
                                TimeStamp = DateTime.UtcNow
                            };

                            await BackOfficeService.AddActivityLog(activityLog);
                        }
                    });
                    Parallel.ForEach(distinctLiveTemplates, async (item) =>
                    {
                        if (_liveSelectedItem != null && _liveSelectedItem.Value.ToString() != item.Id)
                        {
                            var leagues = Leagues.Where(x => x.LiveTemplateId.ToString() == item.Id).ToList();

                            var from = "";
                            var to = "";
                            from = JsonSerializer.Serialize(new ConfigTemplateLogModel()
                            {
                                TemplateName = item.Id != "-1" ? item.Name : "Unassigned",
                                Payloads = new ConfigTemplateLogPayloadModel()
                                {
                                    Clients = leagues.Select(x => x.ClientName).Distinct().ToList(),
                                    Leagues = leagues.Select(x => x.LeagueName).Distinct().ToList()
                                }
                            });
                            to = JsonSerializer.Serialize(new ConfigTemplateLogModel()
                            {
                                TemplateName = _liveSelectedItem != null && _liveSelectedItem.Value.ToString() != "-1" ? _liveSelectedItem.Name : "Unassigned",
                                Payloads = new ConfigTemplateLogPayloadModel()
                                {
                                    Clients = new List<string>(),
                                    Leagues = new List<string>()
                                }
                            });

                            var activityLog = new ActivityLogModel()
                            {
                                Page = SportsbookClientPermission.SBC_OE_Config.ToString().Replace("_", "."),
                                Payload = $"{ActivityLogPayload}.{(int)FixtureStatus.Live}.{(_liveSelectedItem.Value.ToString() == "-1" ? "Removed Current Template" : _liveSelectedItem?.Name ?? "")}.T".Replace(" ", "_"),
                                From = from,
                                To = to,
                                Activity = Activity.Update.ToString(),
                                UserId = UserModel.Id,
                                UserName = UserModel.UserName,
                                TimeStamp = DateTime.UtcNow
                            };

                            await BackOfficeService.AddActivityLog(activityLog);
                        }
                    });

                    var message = "";
                    if (state == ConfigState.UnAssigned)
                    {
                        message = "Template successfully removed. Selected leagues have been moved to the 'Unassigned' page.";
                    }
                    else
                    {
                        message = $"{_prematchSelectedItem?.Name ?? ""} template has successfully been assigned to selected leagues.";
                    }
                    _alertModel = new()
                    {
                        Status = AlertStatus.Success,
                        Message = message,
                    };
                    await Modal.OnSaveClickCallback.InvokeAsync(_alertModel);
                }

                IsPreLoader = true;

            }
            else
            {
                IsPreLoader = false;
            }
            ModalService.Cancel();
        }

        public async Task OnShowAllClickedEvent()
        {
            IsShowAll = !IsShowAll;
        }

        public void Dispose()
        {

        }
        #endregion

        #region Utilities

        private async Task<List<SelectListItemModel<long>>> GetTemplates(int sportId, int scopeId)
        {
            IsPreLoader = true;
            var response = await BackOfficeService.GetConfigTemplate(sportId, scopeId);

            IsPreLoader = false;
            if (response.StatusCode == StatusCode.Success && response.Result.Count > 0)
            {
                return response.Result.Select(x => new SelectListItemModel<long>()
                {
                    Value = x.Id,
                    Name = x.Name
                }).ToList();

            }

            return new List<SelectListItemModel<long>>()
            {
                new SelectListItemModel<long>()
                {
                    Value =- 1,
                    Name = "No Template"
                }
            };
        }

        private async Task<ConfigTemplateMarginModel> GetConfigTemplateMargin(long configTemplateId)
        {
            var response = await BackOfficeService.GetConfigTemplateMargin(configTemplateId);

            if (response.StatusCode == StatusCode.Success)
                return response.Result;
            else
                return new ConfigTemplateMarginModel();

        }

        #endregion
    }
}
