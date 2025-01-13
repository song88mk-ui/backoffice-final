using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.OddsEngine.SportSettings;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.SportSetting;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class SportSettings : IDisposable
    {
        [Parameter] public string Region { get; set; } = "HK";

        private List<BreadcrumbModel> _breadcrumbs = new List<BreadcrumbModel>();
        private List<SportSettingSport> _sportSettings = new List<SportSettingSport>();
        private List<SportSettingSport> _sportLookupSettings = new List<SportSettingSport>();
        private List<SportSettingSport> _editedSportSettings = new List<SportSettingSport>();

        private PageHeaderModel _pageHeaderModel = new PageHeaderModel();
        private AlertModel _alertModel;

        private string _clientCode;
        private string _targetLocation;
        private string _pageUrl = "/hk/odds-engine/sport-settings";

        private bool _processing;
        private bool _init = true;
        private bool _isPreload;
        private bool _isUpdate;
        private bool _hasChanges;
        private bool _allowedLeave;
        private LanguageCode _languageCode;

        private EditSportSettingsPage _currentEditPage;
        private IDisposable registration;

        private Task<BaseResponse<SegmentLookupResponse>> _segmentLookupTask;
        private Task<BaseResponse<LeagueLookupResponse>> _leagueLookupTask;
        protected override async Task OnInitializedAsync()
        {
            if (PreRenderService.IsPreRendering)
                return;

            PageState.Page = this;

            _breadcrumbs.Add(new BreadcrumbModel
            {
                Name = "Sports Settings",
                Url = _pageUrl,
                IsCurrentPage = true
            });

            _pageHeaderModel = new PageHeaderModel
            {
                Title = "Sports Settings",
                ShowLanguageDropDown = false
            };

            _isPreload = true;

            _segmentLookupTask = Task.Run(() => _tradingService.GetSegmentLookup(0, _languageCode));
            _leagueLookupTask = Task.Run(() => _tradingService.GetLeagueLookup(0, 0, _languageCode));

            await Task.CompletedTask;
            _isUpdate = false;
        }
        protected override async Task OnParametersSetAsync()
        {
            if (_init && !string.IsNullOrEmpty(_clientCode) && _languageCode != _translationState.ActiveLanguageCode)
            {
                _languageCode = _translationState.ActiveLanguageCode;
                await GetSports();
            }
            _init = false;
        }
        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                registration = _navigationManager.RegisterLocationChangingHandler(OnLocationChanging);
            }
        }

        private async ValueTask OnLocationChanging(LocationChangingContext context)
        {
            if (!_allowedLeave)
            {
                if (_isUpdate && _hasChanges)
                {
                    var parameters = new ModalParameters();
                    parameters.Add("UserAction", "LEAVING");
                    parameters.Add("DialogStyle", "max-width: 600px;");

                    var options = new ModalOptions();
                    _modalService.Open<ConfirmationModal>("Confirmation", parameters, options);

                    _targetLocation = context.TargetLocation.Replace(_navigationManager.BaseUri, "");
                    context.PreventNavigation();
                }
            }
        }

        private async Task HandleCancelClick()
        {
            if (_currentEditPage != EditSportSettingsPage.SegmentAndLeagues)
            {
                var parameters = new ModalParameters();
                parameters.Add("UserAction", "CANCELLING");
                parameters.Add("DialogStyle", "max-width: 600px;");

                var options = new ModalOptions();
                _modalService.Open<ConfirmationModal>("Confirmation", parameters, options);
            }
        }
        public async Task HandleClientChange(string clientCode)
        {
            _clientCode = clientCode;
            _currentEditPage = EditSportSettingsPage.Main;
            _isUpdate = false;

            await GetSports();
        }
        public async Task HandleUpdateClick(EditSportSettingsPage editSportSettingsPage)
        {
            _isPreload = true;
            if (!_sportLookupSettings.Any())
            {
                var sportLookupResponse = await _tradingService.GetSportLookup(_translationState.ActiveLanguageCode.ToString());

                await Task.WhenAll(_leagueLookupTask, _segmentLookupTask);
                var leagueLookupResponse = _leagueLookupTask.Result.Result;
                var segmentLookupResponse = _segmentLookupTask.Result.Result;

                if (sportLookupResponse.StatusCode == StatusCode.Success && _segmentLookupTask.Result.StatusCode == StatusCode.Success && _leagueLookupTask.Result.StatusCode == StatusCode.Success)
                {
                    var segmentSettings = segmentLookupResponse.SegmentLookup
                    .GroupJoin(leagueLookupResponse.LeagueLookup,
                    segment => new { SpId = segment.SportId, SegId = segment.SegmentId },
                    league => new { SpId = league.SportId, SegId = league.SegmentId },
                    (segment, league) => new SportSettingSegment
                    {
                        SportId = segment.SportId,
                        SegmentId = segment.SegmentId,
                        SegmentName = segment.SegmentName,
                        SortOrder = segment.SegmentId,
                        IsActive = true,
                        Leagues = league.Select(s => new SportSettingLeague
                        {
                            SportId = s.SportId,
                            SegmentId = s.SegmentId,
                            LeagueId = s.LeagueId,
                            LeagueName = s.LeagueName,
                            SortOrder = s.LeagueId,
                            IsActive = true
                        }).ToList()
                    }).ToList();

                    _sportLookupSettings = sportLookupResponse.Result.SportLookup
                        .GroupJoin(segmentSettings,
                        sport => sport.SportId,
                        segment => segment.SportId,
                        (sport, segment) => new { sport, segment })
                        .Select(s => new SportSettingSport
                        {
                            SportId = s.sport.SportId,
                            SportName = s.sport.SportName.TrimEnd(),
                            SortOrder = s.sport.SportId,
                            IsActive = false,
                            IsOpen = false,
                            Segments = s.segment.ToList()
                        })
                        .OrderBy(o => o.SortOrder)
                        .ToList();

                    // add virtual sports
                    _sportLookupSettings.AddRange(GetVirtualSports());
                }
            }

            switch (editSportSettingsPage)
            {
                case EditSportSettingsPage.Sport:
                    _isUpdate = true;
                    _editedSportSettings = new List<SportSettingSport>();
                    _editedSportSettings.AddRange(_sportSettings);
                    break;
                case EditSportSettingsPage.SegmentAndLeagues:
                    _isUpdate = true;
                    _hasChanges = false;
                    var sportsOrder = await _jsRuntime.InvokeAsync<List<SortOrderModel>>("getCurrentSortOrder", "#sportstreeEditable .sport-container");

                    _editedSportSettings = _sportSettings
                        .Join(sportsOrder,
                        setting => setting.SportId,
                        sort => int.Parse(sort.RowId),
                        (setting, sort) => new SportSettingSport
                        {
                            SportId = setting.SportId,
                            SportName = setting.SportName,
                            Segments = setting.Segments,
                            SortOrder = sort.SortOrder,
                            IsActive = true
                        })
                        .ToList();

                    var currentSportIds = _editedSportSettings.Select(s => s.SportId).ToList();
                    var filteredSportsOrder = sportsOrder.Where(c => !currentSportIds.Contains(int.Parse(c.RowId))).ToList();

                    var newSportSettings = _sportLookupSettings
                        .Join(filteredSportsOrder,
                        setting => setting.SportId,
                        sort => int.Parse(sort.RowId),
                        (setting, sort) => new SportSettingSport
                        {
                            SportId = setting.SportId,
                            SportName = setting.SportName,
                            Segments = setting.Segments,
                            SortOrder = sort.SortOrder,
                            IsActive = true
                        })
                        .ToList();

                    _editedSportSettings.AddRange(newSportSettings);

                    _editedSportSettings = _editedSportSettings.OrderBy(o => o.SortOrder).ToList();
                    break;
                case EditSportSettingsPage.Main:
                    await SaveSportsSetting();

                    _isUpdate = false;
                    //_renderFilter = true;
                    break;
            }

            if (_isPreload)
                _isPreload = false;
            _currentEditPage = editSportSettingsPage;
        }
        public async Task HandleCopyClick()
        {
            var parameters = new ModalParameters();

            parameters.Add("FromClientCode", _clientCode);

            var sports = _sportSettings
                .Select(s => new SportsSortOrder
                {
                    SportId = s.SportId,
                    SportName = s.SportName,
                    SortOrder = s.SortOrder,
                    SegmentsSortOrder = s.Segments.Select(se => new SportsSegmentSortOrder
                    {
                        SportId = s.SportId,
                        SportName = s.SportName,
                        SegmentId = se.SegmentId,
                        SegmentName = se.SegmentName,
                        LeagueSortOrder = se.Leagues.Select(le => new SportSegmentLeagueSortOrder
                        {
                            SportId = s.SportId,
                            SportName = s.SportName,
                            SegmentId = se.SegmentId,
                            SegmentName = se.SegmentName,
                            LeagueId = le.LeagueId,
                            LeagueName = le.LeagueName,
                            SortOrder = le.SortOrder,
                            IsActive = le.IsActive
                        }).ToList(),
                        SortOrder = se.SortOrder,
                        IsActive = se.IsActive,
                        IsOpen = se.IsOpen
                    }).ToList(),
                    IsActive = s.IsActive,
                    IsOpen = s.IsOpen
                }).ToList();

            parameters.Add("Sports", sports);
            parameters.Add("DialogStyle", "max-width: 600px;");
            parameters.Add("Region", Region);

            var options = new ModalOptions();

            _modalService.Open<CopyModal>($"Copy Sports Settings", parameters, options);
        }
        public async Task HdleViewLogsClick()
        {
            var parameters = new ModalParameters();

            parameters.Add("ClientCode", _clientCode);

            parameters.Add("DialogStyle", "max-width: 1300px !important;");
            parameters.Add("ModalClass", "show logs-modal");
            parameters.Add("Region", Region);

            var options = new ModalOptions();

            _modalService.Open<ViewLogsModal>("View Logs", parameters, options);
        }
        public async void HandleConfirmationModalSaveClick(object userAction)
        {
            if (userAction == null)
                return;

            switch ((int)userAction)
            {
                case (int)EditSportSettingsPage.Main:
                    //_renderFilter = false;
                    _isUpdate = false;

                    await GetSports();
                    break;
                case (int)EditSportSettingsPage.Sport:
                case (int)EditSportSettingsPage.SegmentAndLeagues:
                    var editablePages = new[] { EditSportSettingsPage.Sport, EditSportSettingsPage.SegmentAndLeagues };
                    if (editablePages.Contains(_currentEditPage))
                    {
                        await SaveSportsSetting();
                    }

                    if (!string.IsNullOrEmpty(_targetLocation))
                    {
                        _allowedLeave = true;
                        _navigationManager.NavigateTo(_targetLocation);
                    }
                    break;
                default:
                    break;
            }
        }
        public async void HandleModalCancelClick(object userAction)
        {
            _allowedLeave = false;
            if (userAction == null)
                return;

            if ((int)userAction == (int)EditSportSettingsPage.Sport)
            {
                if (!string.IsNullOrEmpty(_targetLocation))
                {
                    _allowedLeave = true;
                    _navigationManager.NavigateTo(_targetLocation);
                }
            }
        }


        private async Task GetSports()
        {
            if (_processing)
                return;

            try
            {
                _sportSettings = new List<SportSettingSport>();

                if (string.IsNullOrEmpty(_clientCode))
                    return;

                if (!_isPreload)
                    _isPreload = true;

                var sportsOrderTask = _tradingService.GetClientSportOrderList(new ClientSportOrderListRequest
                {
                    ClientCode = _clientCode
                });

                var segmentOrderTask = _tradingService.GetClientSegmentOrderList(new ClientSegmentOrderListRequest
                {
                    ClientCode = _clientCode
                });

                var leagueOrderTask = _tradingService.GetClientLeagueOrderList(new ClientLeagueOrderListRequest
                {
                    ClientCode = _clientCode
                });

                await Task.WhenAll(sportsOrderTask, segmentOrderTask, leagueOrderTask, _leagueLookupTask, _segmentLookupTask);
                var leagueLookupResponse = _leagueLookupTask.Result.Result;
                var segmentLookupResponse = _segmentLookupTask.Result.Result;
                var sportsOrderResponse = sportsOrderTask.Result;
                var segmentOrderResponse = segmentOrderTask.Result;
                var leagueOrderResponse = leagueOrderTask.Result;

                if (sportsOrderResponse.StatusCode == StatusCode.Success && segmentOrderResponse.StatusCode == StatusCode.Success && leagueOrderResponse.StatusCode == StatusCode.Success)
                {
                    var leagues = leagueLookupResponse.LeagueLookup.GroupJoin(leagueOrderResponse.Result,
                    le => new { le.SportId, le.SegmentId, le.LeagueId },
                    so => new { so.SportId, so.SegmentId, so.LeagueId },
                    (le, so) => new { Lookup = le, LookupOrder = so })
                    .SelectMany(
                    temp => temp.LookupOrder.DefaultIfEmpty(),
                    (temp, s) => new
                    {
                        temp.Lookup,
                        temp.LookupOrder,
                        temp.LookupOrder.FirstOrDefault()?.SortOrder
                    })
                    .GroupBy(x => new { x.Lookup.SportId, x.Lookup.SegmentId })
                    .SelectMany(group =>
                    {
                        var maxSortOrder = group.Max(x => x.SortOrder ?? 0);

                        return group.Select((x, index) => new SportSettingLeague
                        {
                            SportId = x.Lookup.SportId,
                            SegmentId = x.Lookup.SegmentId,
                            LeagueId = x.Lookup.LeagueId,
                            LeagueName = x.Lookup.LeagueName.TrimEnd(),
                            IsActive = x.LookupOrder.FirstOrDefault() != null,
                            SortOrder = x.LookupOrder.FirstOrDefault()?.SortOrder ?? (++maxSortOrder),
                        });
                    })
                    .OrderBy(o => o.SortOrder)
                    .ToList();

                    var segments = segmentLookupResponse.SegmentLookup.GroupJoin(segmentOrderResponse.Result,
                    seg => new { seg.SportId, seg.SegmentId },
                    lp => new { lp.SportId, lp.SegmentId },
                    (seg, lp) => new { Lookup = seg, LookupOrder = lp })
                    .SelectMany(
                    temp => temp.LookupOrder.DefaultIfEmpty(),
                    (temp, s) => new
                    {
                        temp.Lookup,
                        temp.LookupOrder,
                        temp.LookupOrder.FirstOrDefault()?.SortOrder
                    })
                    .GroupBy(x => new { x.Lookup.SportId })
                    .SelectMany(group =>
                    {
                        var maxSortOrder = group.Max(x => x.SortOrder ?? 0);

                        return group.Select((x, index) => new SportSettingSegment
                        {
                            SportId = x.Lookup.SportId,
                            SegmentId = x.Lookup.SegmentId,
                            SegmentName = x.Lookup.SegmentName.TrimEnd(),
                            IsActive = x.LookupOrder.FirstOrDefault() != null,
                            SortOrder = x.LookupOrder.FirstOrDefault()?.SortOrder ?? (++maxSortOrder),
                            IsOpen = false,
                            Leagues = leagues.Where(c => c.SportId == x.Lookup.SportId && c.SegmentId == x.Lookup.SegmentId).ToList()
                        });
                    })
                    .OrderBy(o => o.SortOrder)
                    .ToList();

                    _sportSettings = sportsOrderResponse.Result
                        .Where(s => s.IsActive)
                        .GroupJoin(segments,
                        sport => sport.SportId,
                        segment => segment.SportId,
                        (sport, segment) => new { sport, segment })
                        .Select(s => new SportSettingSport
                        {
                            SportId = s.sport.SportId,
                            SportName = s.sport.SportName.TrimEnd(),
                            SortOrder = s.sport.SortOrder,
                            IsActive = s.sport.IsActive,
                            IsOpen = false,
                            Segments = s.segment.ToList()
                        })
                        .OrderBy(o => o.SortOrder)
                        .ToList();
                }

                _isPreload = false;
                StateHasChanged();
            }
            finally
            {
                _processing = false;
            }
        }
        private List<SportsSortOrder> GetSportSortOrder(List<SportSettingSport> sportSettings)
        {
            var result = sportSettings.Select((s, sIndex) => new SportsSortOrder
            {
                SportId = s.SportId,
                SportName = s.SportName,
                SortOrder = sIndex + 1,
                SegmentsSortOrder = s.Segments.Select((se, seIndex) => new SportsSegmentSortOrder
                {
                    SportId = s.SportId,
                    SportName = s.SportName,
                    SegmentId = se.SegmentId,
                    SegmentName = se.SegmentName,
                    LeagueSortOrder = se.Leagues.Select((le, leIndex) => new SportSegmentLeagueSortOrder
                    {
                        SportId = s.SportId,
                        SportName = s.SportName,
                        SegmentId = se.SegmentId,
                        SegmentName = se.SegmentName,
                        LeagueId = le.LeagueId,
                        LeagueName = le.LeagueName,
                        SortOrder = leIndex + 1,
                        IsActive = le.IsActive
                    }).ToList(),
                    SortOrder = seIndex + 1,
                    IsActive = se.IsActive,
                    IsOpen = se.IsOpen
                }).ToList(),
                IsActive = s.IsActive,
                IsOpen = s.IsOpen
            }).ToList();

            return result;
        }
        private List<SportSettingSport> GetVirtualSports()
        {
            // 1001,1002,1003,1005,1021,1055,1058
            var sports = new List<SportSettingSport>
            {
                new SportSettingSport
                {
                    SportId = 1001,
                    SportName = "Virtual Soccer",
                    SortOrder = 1001
                },

                new SportSettingSport
                {
                    SportId = 1002,
                    SportName = "Virtual Basketball",
                    SortOrder = 1002
                },

                new SportSettingSport
                {
                    SportId = 1003,
                    SportName = "Virtual Baseball",
                    SortOrder = 1003
                },

                new SportSettingSport
                {
                    SportId = 1005,
                    SportName = "Virtual Tennis",
                    SortOrder = 1005
                },

                new SportSettingSport
                {
                    SportId = 1021,
                    SportName = "Virtual Cricket",
                    SortOrder = 1021
                },

                new SportSettingSport
                {
                    SportId = 1055,
                    SportName = "Virtual Horse Classics",
                    SortOrder = 1055
                },

                new SportSettingSport
                {
                    SportId = 1058,
                    SportName = "Virtual Dog Racing",
                    SortOrder = 1058
                }
            };

            return sports;
        }
        private async Task SaveSportsSetting()
        {
            _isPreload = true;
            var newSportOrder = GetSportSortOrder(_editedSportSettings);

            var changes = GetSortOrderChanges(_sportSettings, newSportOrder);

            if (changes.Any())
            {
                var activityLog = GetActivityLogModel(changes, _sportSettings, newSportOrder);

                var response = await _tradingService.AddOrUpdateClientSportSettings(new ClientSportSettingRequest
                {
                    ClientCode = _clientCode,
                    UserId = _userState.Id,
                    UserName = _userState.Name,
                    TimeStamp = DateTime.UtcNow,
                    SportsSortOrder = newSportOrder
                });

                if (response.StatusCode == StatusCode.Success)
                {
                    await _backofficeService.AddActivityLog(activityLog);
                    _alertModel = new AlertModel()
                    {
                        Status = AlertStatus.Success,
                        Message = "Success! Your changes have been saved.",
                    };
                }
            }

            //_renderFilter = false;
            _isUpdate = false;

            await GetSports();
        }
        public List<SortOrderChange> GetSortOrderChanges(List<SportSettingSport> currentRequest, List<SportsSortOrder> newRequest)
        {
            var changes = new List<SortOrderChange>();

            if (currentRequest == null || newRequest == null)
                return changes;

            foreach (var newSport in newRequest)
            {
                var currentSport = currentRequest.FirstOrDefault(s => s.SportId == newSport.SportId);
                if (currentSport == null || currentSport.SortOrder != newSport.SortOrder || currentSport.IsActive != newSport.IsActive)
                {
                    changes.Add(new SortOrderChange
                    {
                        ChangeType = "Sport",
                        SportId = newSport.SportId
                    });

                    if (currentSport == null)
                        continue;
                }

                foreach (var newSegment in newSport.SegmentsSortOrder)
                {
                    var currentSegment = currentSport?.Segments.FirstOrDefault(s => s.SegmentId == newSegment.SegmentId);
                    if (currentSegment == null || currentSegment.SortOrder != newSegment.SortOrder || currentSegment.IsActive != newSegment.IsActive)
                    {
                        changes.Add(new SortOrderChange
                        {
                            ChangeType = "Segment",
                            SportId = newSport.SportId,
                            SegmentId = newSegment.SegmentId
                        });

                        if (currentSegment == null)
                            continue;
                    }

                    foreach (var newLeague in newSegment.LeagueSortOrder)
                    {
                        var currentLeague = currentSegment?.Leagues.FirstOrDefault(l => l.LeagueId == newLeague.LeagueId);
                        if (currentLeague == null || currentLeague.SortOrder != newLeague.SortOrder || currentLeague.IsActive != newLeague.IsActive)
                        {
                            changes.Add(new SortOrderChange
                            {
                                ChangeType = "League",
                                SportId = newSport.SportId,
                                SegmentId = newSegment.SegmentId,
                                LeagueId = newLeague.LeagueId
                            });
                        }
                    }
                }
            }

            foreach (var currentSport in currentRequest)
            {
                if (!newRequest.Any(s => s.SportId == currentSport.SportId))
                {
                    changes.Add(new SortOrderChange
                    {
                        ChangeType = "Sport",
                        SportId = currentSport.SportId
                    });

                    continue;
                }

                foreach (var currentSegment in currentSport.Segments)
                {
                    if (!newRequest.Any(ns => ns.SportId == currentSport.SportId && ns.SegmentsSortOrder.Any(nss => nss.SegmentId == currentSegment.SegmentId)))
                    {
                        changes.Add(new SortOrderChange
                        {
                            ChangeType = "Segment",
                            SportId = currentSport.SportId,
                            SegmentId = currentSegment.SegmentId
                        });

                        continue;
                    }

                    foreach (var currentLeague in currentSegment.Leagues)
                    {
                        if (!newRequest.Any(ns => ns.SportId == currentSport.SportId &&
                                                  ns.SegmentsSortOrder.Any(nss => nss.SegmentId == currentSegment.SegmentId &&
                                                  nss.LeagueSortOrder.Any(nl => nl.LeagueId == currentLeague.LeagueId)))
                        )
                        {
                            changes.Add(new SortOrderChange
                            {
                                ChangeType = "League",
                                SportId = currentSport.SportId,
                                SegmentId = currentSegment.SegmentId,
                                LeagueId = currentLeague.LeagueId
                            });
                        }
                    }
                }
            }

            return changes;
        }
        public ActivityLogModel GetActivityLogModel(List<SortOrderChange> changes, List<SportSettingSport> currentSportOrder, List<SportsSortOrder> newSportOrder)
        {
            var payloadFrom = new SportSettingSortOrderAudit();
            var payloadTo = new SportSettingSortOrderAudit();

            if (changes.Any(c => c.ChangeType == "Sport"))
            {
                payloadFrom.Sports = _sportSettings.Select(s => new SportSortOrderAudit
                {
                    SportId = s.SportId,
                    SportName = s.SportName,
                    SortOrder = s.SortOrder
                }).ToList();

                payloadTo.Sports = newSportOrder.Select(s => new SportSortOrderAudit
                {
                    SportId = s.SportId,
                    SportName = s.SportName,
                    SortOrder = s.SortOrder
                }).ToList();
            }

            var segmentChanges = changes.Where(c => c.ChangeType == "Segment").Select(s => s.SportId).Distinct().ToHashSet();
            if (segmentChanges.Any())
            {
                payloadFrom.Segments = _sportSettings
                    .Where(c => segmentChanges.Contains(c.SportId))
                    .Select(sport => new SegmentSortOrderAuditSport
                    {
                        SportId = sport.SportId,
                        SportName = sport.SportName,
                        Segments = sport.Segments.Select(segment => new SegmentSortOrderAuditSegment
                        {
                            SegmentId = segment.SegmentId,
                            SegmentName = segment.SegmentName.TrimEnd(),
                            SortOrder = segment.SortOrder
                        }).ToList()
                    }).ToList();

                payloadTo.Segments = newSportOrder
                    .Where(c => segmentChanges.Contains(c.SportId))
                    .Select(sport => new SegmentSortOrderAuditSport
                    {
                        SportId = sport.SportId,
                        SportName = sport.SportName,
                        Segments = sport.SegmentsSortOrder.Select(segment => new SegmentSortOrderAuditSegment
                        {
                            SegmentId = segment.SegmentId,
                            SegmentName = segment.SegmentName.TrimEnd(),
                            SortOrder = segment.SortOrder
                        }).ToList()
                    }).ToList();
            }

            var leagueChanges = changes
                .Where(c => c.ChangeType == "League")
                .Select(s => new { s.SportId, s.SegmentId })
                .Distinct()
                .GroupBy(x => x.SportId)
                .Select(g => new
                {
                    SportId = g.Key,
                    SegmentIds = g.Select(x => x.SegmentId).ToList()
                })
                .ToList();

            if (leagueChanges.Any())
            {
                payloadFrom.Leagues = _sportSettings
                    .Where(sport => leagueChanges.Any(lc => lc.SportId == sport.SportId))
                    .Select(sport => new LeagueSortOrderAuditSport
                    {
                        SportId = sport.SportId,
                        SportName = sport.SportName,
                        Segments = sport.Segments
                            .Where(segment => leagueChanges.First(lc => lc.SportId == sport.SportId).SegmentIds.Contains(segment.SegmentId))
                            .Select(segment => new LeagueSortOrderAuditSegment
                            {
                                SegmentId = segment.SegmentId,
                                SegmentName = segment.SegmentName,
                                Leagues = segment.Leagues.Select(league => new LeagueSortOrderAuditLeague
                                {
                                    LeagueId = league.LeagueId,
                                    LeagueName = league.LeagueName.TrimEnd(),
                                    SortOrder = league.SortOrder
                                }).ToList()
                            }).ToList()
                    }).ToList();

                payloadTo.Leagues = newSportOrder
                    .Where(sport => leagueChanges.Any(lc => lc.SportId == sport.SportId))
                    .Select(sport => new LeagueSortOrderAuditSport
                    {
                        SportId = sport.SportId,
                        SportName = sport.SportName,
                        Segments = sport.SegmentsSortOrder
                            .Where(segment => leagueChanges.First(lc => lc.SportId == sport.SportId).SegmentIds.Contains(segment.SegmentId))
                            .Select(segment => new LeagueSortOrderAuditSegment
                            {
                                SegmentId = segment.SegmentId,
                                SegmentName = segment.SegmentName,
                                Leagues = segment.LeagueSortOrder.Select(league => new LeagueSortOrderAuditLeague
                                {
                                    LeagueId = league.LeagueId,
                                    LeagueName = league.LeagueName.TrimEnd(),
                                    SortOrder = league.SortOrder
                                }).ToList()
                            }).ToList()
                    }).ToList();
            }

            return new ActivityLogModel
            {
                Page = SportsbookClientPermission.SBC_OE_SportsSettings.ToString().Replace('_', '.'),
                Activity = Activity.Update.ToString(),
                Payload = $"{_clientCode}",
                From = JsonSerializer.Serialize(payloadFrom),
                To = JsonSerializer.Serialize(payloadTo),
                UserId = _userState.Id,
                UserName = _userState.Name
            };
        }
        public void Dispose() => registration?.Dispose();
    }
}
