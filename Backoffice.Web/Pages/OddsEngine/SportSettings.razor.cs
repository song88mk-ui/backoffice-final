using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.OddsEngine.SportSettings;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class SportSettings : IDisposable
    {
        [Inject] public IPreRenderService PreRenderService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }
        [Parameter] public string Region { get; set; } = "hk";

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        private List<BreadcrumbModel> _breadcrumbs = new List<BreadcrumbModel>();
        private List<SportSettingSport> _sportSettings = new List<SportSettingSport>();
        private List<SportSettingSport> _sportLookupSettings = new List<SportSettingSport>();
        private List<SportSettingSport> _editedSportSettings = new List<SportSettingSport>();

        private PageHeaderModel _pageHeaderModel = new PageHeaderModel();
        private AlertModel _alertModel;

        private string _clientCode;
        private string _targetLocation;
        private string _pageUrl = "/hk/odds-engine/sport-settings";

        //private bool _renderFilter = true;
        private bool _isPreload;
        private bool _isUpdate;
        private bool _hasChanges;
        private bool _allowedLeave;

        private EditSportSettingsPage _currentEditPage;
        private IDisposable registration;

        protected override async Task OnInitializedAsync()
        {
            if (PreRenderService.IsPreRendering)
                return;

            _breadcrumbs.Add(new BreadcrumbModel
            {
                Name = "Sport Settings",
                Url = _pageUrl,
                IsCurrentPage = true
            });

            _pageHeaderModel = new PageHeaderModel
            {
                Title = "Sport Settings"
            };
            await Task.CompletedTask;
            _isUpdate = false;
        }
        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                registration = NavigationManager.RegisterLocationChangingHandler(OnLocationChanging);
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
                    ModalService.Open<ConfirmationModal>("Confirmation", parameters, options);

                    _targetLocation = context.TargetLocation.Replace(NavigationManager.BaseUri, "");
                    context.PreventNavigation();
                }
            }
        }
        private async Task OnCancelClickedEvent()
        {
            if (_currentEditPage != EditSportSettingsPage.SegmentAndLeagues)
            {
                var parameters = new ModalParameters();
                parameters.Add("UserAction", "CANCELLING");
                parameters.Add("DialogStyle", "max-width: 600px;");

                var options = new ModalOptions();
                ModalService.Open<ConfirmationModal>("Confirmation", parameters, options);
            }
        }
        public async Task OnClientChangedEvent(string clientCode)
        {
            _clientCode = clientCode;
            _currentEditPage = EditSportSettingsPage.Main;
            //_renderFilter = false;
            _isUpdate = false;

            await GetSports();
        }

        public async Task OnUpdateClickEvent(EditSportSettingsPage editSportSettingsPage)
        {
            _isPreload = true;
            if (!_sportLookupSettings.Any())
            {
                var sportLookupResponse = await TradingService.GetSportLookup();
                var segmentLookupResponse = await TradingService.GetSegmentLookup(0);
                var leagueLookupResponse = await TradingService.GetLeagueLookup(0, 0);

                if (sportLookupResponse.StatusCode == StatusCode.Success && segmentLookupResponse.StatusCode == StatusCode.Success && leagueLookupResponse.StatusCode == StatusCode.Success)
                {
                    var segmentSettings = segmentLookupResponse.Result.SegmentLookup
                    .GroupJoin(leagueLookupResponse.Result.LeagueLookup,
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
                    break;
                case EditSportSettingsPage.SegmentAndLeagues:
                    _isUpdate = true;
                    _hasChanges = false;
                    var sportsOrder = await JSRuntime.InvokeAsync<List<SortOrderModel>>("getCurrentSortOrder", "#sportstreeEditable .sport-container");

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
        public async Task OnCopyClickEvent()
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

            ModalService.Open<CopyModal>($"Copy Sports Settings", parameters, options);
        }
        public async Task OnViewLogsClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("ClientCode", _clientCode);

            parameters.Add("DialogStyle", "max-width: 1300px !important;");
            parameters.Add("ModalClass", "show logs-modal");
            parameters.Add("Region", Region);

            var options = new ModalOptions();

            ModalService.Open<ViewLogsModal>("View Logs", parameters, options);
        }
        public async void OnConfirmationModalSaveClicked(object userAction)
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
                        NavigationManager.NavigateTo(_targetLocation);
                    }
                    break;
                default:
                    break;
            }
        }

        public async void OnModalCancelClicked(object userAction)
        {
            _allowedLeave = false;
            if (userAction == null)
                return;

            if ((int)userAction == (int)EditSportSettingsPage.Sport)
            {
                if (!string.IsNullOrEmpty(_targetLocation))
                {
                    _allowedLeave = true;
                    NavigationManager.NavigateTo(_targetLocation);
                }
            }
        }

        private async Task GetSports()
        {
            _sportSettings = new List<SportSettingSport>();

            if (string.IsNullOrEmpty(_clientCode))
                return;

            if (!_isPreload)
                _isPreload = true;

            var sportsOrderResponse = await TradingService.GetClientSportOrderList(new ClientSportOrderListRequest
            {
                ClientCode = _clientCode
            });

            var segmentOrderResponse = await TradingService.GetClientSegmentOrderList(new ClientSegmentOrderListRequest
            {
                ClientCode = _clientCode
            });

            var leagueOrderResponse = await TradingService.GetClientLeagueOrderList(new ClientLeagueOrderListRequest
            {
                ClientCode = _clientCode
            });

            if (sportsOrderResponse.StatusCode == StatusCode.Success && segmentOrderResponse.StatusCode == StatusCode.Success && leagueOrderResponse.StatusCode == StatusCode.Success)
            {
                var segmentSettings = segmentOrderResponse.Result
                    .GroupJoin(leagueOrderResponse.Result,
                    segment => new { SpId = segment.SportId, SegId = segment.SegmentId },
                    league => new { SpId = league.SportId, SegId = league.SegmentId },
                    (segment, league) => new SportSettingSegment
                    {
                        SportId = segment.SportId,
                        SegmentId = segment.SegmentId,
                        SegmentName = segment.SegmentName,
                        SortOrder = segment.SortOrder,
                        IsActive = true,
                        Leagues = league.Select(s => new SportSettingLeague
                        {
                            SportId = s.SportId,
                            SegmentId = s.SegmentId,
                            LeagueId = s.LeagueId,
                            LeagueName = s.LeagueName,
                            SortOrder = s.SortOrder,
                            IsActive = true
                        }).ToList()
                    }).ToList();


                _sportSettings = sportsOrderResponse.Result
                    .Where(s => s.IsActive)
                    .GroupJoin(segmentSettings,
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
            await InvokeAsync(StateHasChanged);
            //_renderFilter = true;
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
            var response = await TradingService.AddOrUpdateClientSportSettings(new ClientSportSettingRequest
            {
                ClientCode = _clientCode,
                UserId = UserModel.Id,
                UserName = UserModel.UserName,
                TimeStamp = DateTime.UtcNow,
                SportsSortOrder = GetSportSortOrder(_editedSportSettings),
            });

            if (response.StatusCode == StatusCode.Success)
            {
                _alertModel = new AlertModel()
                {
                    Status = AlertStatus.Success,
                    Message = "Success! Your changes have been saved.",
                };
            }

            //_renderFilter = false;
            _isUpdate = false;

            await GetSports();
        }

        public void Dispose() => registration?.Dispose();
    }
}
