using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class SportSettings2
    {
        [Inject] public IPreRenderService PreRenderService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        private List<BreadcrumbModel> _breadcrumbs = new List<BreadcrumbModel>();
        private PageHeaderModel _pageHeaderModel = new PageHeaderModel();
        private List<SportSettingSport> _sportSettings = new List<SportSettingSport>();
        private List<SportSettingSport> _sportLookupSettings = new List<SportSettingSport>();
        private List<SportSettingSport> _editedSportSettings = new List<SportSettingSport>();
        private AlertModel _alertModel;

        private string _clientCode;

        private bool _renderFilter = true;
        private bool _isPreload;
        private bool _isUpdate;

        private EditSportSettingsPage _currentEditPage;

        protected override async Task OnInitializedAsync()
        {
            if (PreRenderService.IsPreRendering)
                return;

            _breadcrumbs.Add(new BreadcrumbModel
            {
                Name = "Sport Settings",
                Url = $"hk/odds-engine/sport-settings",
                IsCurrentPage = true
            });

            _pageHeaderModel = new PageHeaderModel
            {
                Title = "Sport Settings"
            };
        }

        public async Task OnClientChangedEvent(string clientCode)
        {
            _clientCode = clientCode;

            _renderFilter = false;
            _isUpdate = false;

            await GetSports();
        }

        public async Task OnUpdateClickEvent(Tuple<bool, EditSportSettingsPage> data)
        {
            _isUpdate = data.Item1;

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
                        .Select(s => new SportSettingSport
                        {
                            SportId = s.SportId,
                            SportName = s.SportName,
                            SortOrder = s.SportId
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
                }
            }

            if (data.Item2 == EditSportSettingsPage.SegmentAndLeagues)
            {
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
            }
            else if (data.Item2 == EditSportSettingsPage.Main)
            {
                _isPreload = true;

                var response = await TradingService.AddOrUpdateClientSportSettings(new ClientSportSettingRequest
                {
                    ClientCode = _clientCode,
                    UserId = UserModel.Id,
                    UserName = UserModel.UserName,
                    TimeStamp = DateTime.UtcNow,
                    SportsSortOrder = GetSportSortOrder(),
                });

                _isPreload = false;

                if (response.StatusCode == StatusCode.Success)
                {
                    _sportSettings = _editedSportSettings;

                    _alertModel = new AlertModel()
                    {
                        Status = AlertStatus.Success,
                        Message = "Successfully saved Sport - Segment - League ordering.",
                    };
                }
            }

            _currentEditPage = data.Item2;
        }
        public void OnConfirmationModalSaveClicked(object userAction)
        {
            switch ((int)userAction)
            {
                case 0:
                    break;
                case 1:
                    break;
                default:
                    break;
            }
        }
        private async Task GetSports()
        {
            _sportSettings = new List<SportSettingSport>();

            if (string.IsNullOrEmpty(_clientCode))
                return;

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
        }

        private List<SportsSortOrder> GetSportSortOrder()
        {
            var result = _editedSportSettings.Select((s, sIndex) => new SportsSortOrder
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
    }
}
