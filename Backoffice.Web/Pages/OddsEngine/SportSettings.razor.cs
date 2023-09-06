using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.OddsEngine.SportSettings;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class SportSettings : IAsyncDisposable, IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }
        [Parameter] public string Region { get; set; } = "hk";

        public IJSStreamReference? DataReference { get; set; }
        #endregion

        #region Fields
        private PageHeaderModel _pageHeaderModel;
        private static Func<int, int, Task> OnSegmentActionAsync;
        private static Func<string, int, Task> OnUpdateSortOrderActionAsync;
        private List<BreadcrumbModel> _breadcrumbs;
        private List<SportSettingSport> _modifiedClientSportsSettingsModel;
        private List<SportSettingSport> _clientSportsSettingsModel;
        private Task<BaseResponse<SegmentLookupResponse>> SegmentLookupTask;
        private Task<BaseResponse<LeagueLookupResponse>> LeagueLookupTask;
        private string _tableClass;
        private string _clientCode;
        private AlertModel _alertModel;
        private bool _isUpdate;
        private bool _isPreload;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _pageHeaderModel = new()
            {
                Title = "Sport Settings"
            };

            _tableClass = "sport-setting-table";

            _clientSportsSettingsModel = new List<SportSettingSport>();
            _modifiedClientSportsSettingsModel = new List<SportSettingSport>();
            _alertModel = new AlertModel();


            OnSegmentActionAsync = OnSegmentClickEvent;
            OnUpdateSortOrderActionAsync = OnUpdateSortOrder;

            _breadcrumbs = new List<BreadcrumbModel>();

            _breadcrumbs.Add(new BreadcrumbModel
            {
                Name = "Sport Settings",
                Url = $"{Region}/odds-engine/sport-settings",
                IsCurrentPage = true
            });

            await AuthenticateAccess.InvokeAsync();

            SegmentLookupTask = Task.Run(() => TradingService.GetSegmentLookup(0));
            LeagueLookupTask = Task.Run(() => TradingService.GetLeagueLookup(0, 0));
        }
        protected override void OnParametersSet()
        {
            if (TradingService != null && Region != TradingService.Region)
            {
                TradingService.Region = Region.ToUpper();
            }
        }
        public async Task OnClientChangedEvent(string clientCode)
        {
            _clientCode = clientCode;

            _isPreload = true;

            await GetSports();

            _isPreload = false;
        }

        public async Task OnUpdateClickEvent()
        {
            _isUpdate = true;

            await JSRuntime.InvokeVoidAsync("unloadItems", $".{_tableClass}", ".segment-row");
            await JSRuntime.InvokeVoidAsync("unloadItems", $".{_tableClass}", ".league-row");

            await InitializeSortTable("> tr.sport-row", ".sport-row");
            await JSRuntime.InvokeVoidAsync("enableSort", $".{_tableClass}");
        }

        public async Task OnSaveClickEvent()
        {
            _isPreload = true;
            _isUpdate = false;

            var response = await TradingService.AddOrUpdateClientSportSettings(new ClientSportSettingRequest
            {
                ClientCode = _clientCode,
                UserId = UserModel.Id,
                UserName = UserModel.UserName,
                TimeStamp = DateTime.UtcNow,
                SportsSortOrder = GetSportSortOrder(),
            });

            if (response.StatusCode == StatusCode.Success)
            {
                //_clientSportsSettingsModel = _modifiedClientSportsSettingsModel
                //.Select((s, spIndex) => new SportSettingSport
                //{
                //    SportId = s.SportId,
                //    SportName = s.SportName,
                //    SortOrder = spIndex + 1,
                //    Segments = s.Segments.Select((se, seIndex) => new SportSettingSegment
                //    {
                //        SegmentName = se.SegmentName,
                //        Leagues = se.Leagues.Select((le, leIndex) => new SportSettingLeague
                //        {
                //            LeagueId = le.LeagueId,
                //            LeagueName = le.LeagueName,
                //            SortOrder = leIndex + 1,
                //            IsActive = le.IsActive
                //        }).ToList(),
                //        SortOrder = seIndex + 1,
                //        IsActive = se.IsActive,
                //        IsOpen = se.IsOpen
                //    }).ToList(),
                //    IsActive = s.IsActive,
                //    IsOpen = s.IsOpen
                //}).ToList();

                //_clientSportsSettingsModel = _modifiedClientSportsSettingsModel;

                await GetSports();

                _alertModel = new()
                {
                    Status = AlertStatus.Success,
                    Message = "Successfully saved Sport - Segment - League ordering."
                };
            }
            else
            {
                _alertModel = new()
                {
                    Status = AlertStatus.Error,
                    Message = response.Message
                };
            }


            _isPreload = false;
        }

        public async Task OnCancelClickEvent()
        {
            _isUpdate = false;

            await JSRuntime.InvokeVoidAsync("unloadItems", $".{_tableClass}", ".segment-row");
            await JSRuntime.InvokeVoidAsync("unloadItems", $".{_tableClass}", ".league-row");

            await JSRuntime.InvokeVoidAsync("destroySort", $".{_tableClass}");

            _isPreload = true;

            StateHasChanged();

            await GetSports();

            _isPreload = false;
        }

        public async Task OnCopyClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("FromClientCode", _clientCode);

            var sports = _clientSportsSettingsModel
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

        public async Task OnActiveHeaderChangedEvent(ChangeEventArgs e)
        {
            var isActive = (bool)e.Value;

            _clientSportsSettingsModel = _clientSportsSettingsModel
                .Select(s =>
                {
                    s.IsActive = (bool)e.Value;

                    return s;
                }).ToList();
        }

        public async Task OnSportClickEvent(SportSettingSport model)
        {
            await JSRuntime.InvokeVoidAsync("unloadItems", $".{_tableClass}", ".segment-row");
            await JSRuntime.InvokeVoidAsync("unloadItems", $".{_tableClass}", ".league-row");

            // verify if there are any open rows for segment
            // if there is open then close it.
            if (_clientSportsSettingsModel.Any(c => c.SportId != model.SportId && c.IsOpen))
            {
                var sport = _clientSportsSettingsModel.FirstOrDefault(c => c.SportId != model.SportId && c.IsOpen);
                sport.IsOpen = false;
            }

            if (_isUpdate)
            {
                await JSRuntime.InvokeVoidAsync("destroySort", $".{_tableClass}");

                if (model.IsOpen)
                {
                    model.IsOpen = false;

                    await JSRuntime.InvokeVoidAsync("collapseButton", ".sport-row");
                    await JSRuntime.InvokeVoidAsync("unloadItems", $".{_tableClass}", ".segment-row");

                    await InitializeSortTable("> tr.sport-row", ".sport-row");
                }
                else
                {
                    model.IsOpen = true;

                    // fetch the records on ModifiedClientSportsSettingsModel since this is the record that's being modified
                    var modSport = _modifiedClientSportsSettingsModel.FirstOrDefault(c => c.SportId == model.SportId);
                    var segments = modSport.Segments.Select((s, index) => new
                    {
                        Id = $"segment-{s.SportId}-{s.SegmentId}",
                        SegmentName = s.SegmentName,
                        SortOrder = s.SortOrder,
                        RowClass = "segment-row"
                    }).ToList();

                    await JSRuntime.InvokeVoidAsync("loadSegmentItems", $"#sport-{model.SportId}", segments);
                    await InitializeSortTable("> tr.segment-row", ".segment-row");
                }
            }
            else
            {
                if (model.IsOpen)
                {
                    model.IsOpen = false;

                    await JSRuntime.InvokeVoidAsync("collapseButton", ".sport-row");
                    await JSRuntime.InvokeVoidAsync("unloadItems", $".{_tableClass}", ".segment-row");
                }
                else
                {
                    model.IsOpen = true;

                    var modSport = _clientSportsSettingsModel.FirstOrDefault(c => c.SportId == model.SportId);
                    var segments = modSport.Segments.Select((s, index) => new
                    {
                        Id = $"segment-{s.SportId}-{s.SegmentId}",
                        SegmentName = s.SegmentName,
                        SortOrder = s.SortOrder,
                        RowClass = "segment-row"
                    }).ToList();

                    await JSRuntime.InvokeVoidAsync("loadSegmentItems", $"#sport-{model.SportId}", segments);
                }
            }
        }

        public async Task OnSaveCopyClickEvent()
        {
            _alertModel = new()
            {
                Status = AlertStatus.Success,
                Message = "Sport settings copied successfully."
            };
        }

        public async Task OnSegmentClickEvent(int sportId, int segmentId)
        {
            await JSRuntime.InvokeVoidAsync("unloadItems", $".{_tableClass}", ".league-row");

            var sport = _clientSportsSettingsModel.FirstOrDefault(c => c.SportId == sportId);
            var model = sport.Segments.FirstOrDefault(c => c.SegmentId == segmentId);

            // verify if there are any open rows for leagues
            // if there is open then close it.
            var segment = sport.Segments.Where(c => c.SegmentId != model.SegmentId && c.IsOpen).ToList();
            if (segment != null)
            {
                segment.ForEach(x => x.IsOpen = false);
            }

            if (_isUpdate)
            {
                await JSRuntime.InvokeVoidAsync("destroySort", $".{_tableClass}");

                if (model.IsOpen)
                {
                    model.IsOpen = false;

                    await JSRuntime.InvokeVoidAsync("collapseButton", ".segment-row");
                    await JSRuntime.InvokeVoidAsync("unloadItems", $".{_tableClass}", ".league-row");

                    await InitializeSortTable("> tr.segment-row", ".segment-row");
                }
                else
                {
                    model.IsOpen = true;

                    // fetch the records on ModifiedClientSportsSettingsModel since this is the record that's being modified
                    var modSport = _modifiedClientSportsSettingsModel.FirstOrDefault(c => c.SportId == sportId);
                    var modSegment = modSport.Segments.FirstOrDefault(c => c.SegmentId == model.SegmentId);

                    var leagues = modSegment.Leagues.Select((s, index) => new
                    {
                        Id = $"league-{sportId}-{model.SegmentId}-{s.LeagueId}",
                        LeagueName = s.LeagueName,
                        SortOrder = s.SortOrder,
                        RowClass = "league-row"
                    }).ToList();

                    await JSRuntime.InvokeVoidAsync("loadLeagueItems", $"#segment-{sportId}-{model.SegmentId}", leagues);
                    await InitializeSortTable("> tr.league-row", ".league-row");
                }
            }
            else
            {
                if (model.IsOpen)
                {
                    model.IsOpen = false;

                    await JSRuntime.InvokeVoidAsync("collapseButton", ".segment-row");
                    await JSRuntime.InvokeVoidAsync("unloadItems", $".{_tableClass}", ".league-row");
                }
                else
                {
                    model.IsOpen = true;

                    var modSport = _clientSportsSettingsModel.FirstOrDefault(c => c.SportId == sportId);
                    var modSegment = modSport.Segments.FirstOrDefault(c => c.SegmentId == model.SegmentId);

                    var leagues = modSegment.Leagues.Select((s, index) => new
                    {
                        Id = $"league-{sportId}-{model.SegmentId}-{s.LeagueId}",
                        LeagueName = s.LeagueName,
                        SortOrder = s.SortOrder,
                        RowClass = "league-row"
                    }).ToList();

                    await JSRuntime.InvokeVoidAsync("loadLeagueItems", $"#segment-{sportId}-{model.SegmentId}", leagues);
                }
            }
        }

        public async Task OnUpdateSortOrder(string id, int index)
        {
            // update the index of an element inside the list which is (ModifiedClientSportsSettingsModel)
            var splitId = id.Split('-');
            if (splitId[0].ToLower() == "sport")
            {
                var sportId = int.Parse(splitId[1]);
                var sport = _modifiedClientSportsSettingsModel.FirstOrDefault(c => c.SportId == sportId);

                _modifiedClientSportsSettingsModel.Remove(sport);
                _modifiedClientSportsSettingsModel.Insert(index, sport);
            }
            else if (splitId[0].ToLower() == "segment")
            {
                var sportId = int.Parse(splitId[1]);
                var sport = _modifiedClientSportsSettingsModel.FirstOrDefault(c => c.SportId == sportId);

                var segmentId = int.Parse(splitId[2]);
                var segment = sport.Segments.FirstOrDefault(c => c.SegmentId == segmentId);

                sport.Segments.Remove(segment);
                sport.Segments.Insert(index, segment);
            }
            else if (splitId[0].ToLower() == "league")
            {
                var sportId = int.Parse(splitId[1]);
                var sport = _modifiedClientSportsSettingsModel.FirstOrDefault(c => c.SportId == sportId);

                var segmentId = int.Parse(splitId[2]);
                var segment = sport.Segments.FirstOrDefault(c => c.SegmentId == segmentId);

                var leagueId = int.Parse(splitId[3]);
                var league = segment.Leagues.FirstOrDefault(c => c.LeagueId == leagueId);

                segment.Leagues.Remove(league);
                segment.Leagues.Insert(index, league);
            }
        }

        [JSInvokable]
        public static async Task OnInvokeSegmentClick(string id)
        {
            var splitId = id.Split("-");

            var sportId = int.Parse(splitId[1]);
            var segmentId = int.Parse(splitId[2]);

            await OnSegmentActionAsync.Invoke(sportId, segmentId);
        }

        [JSInvokable]
        public static async Task OnStopSortOrder(string id, int index)
        {
            await OnUpdateSortOrderActionAsync.Invoke(id, index);
        }

        public async ValueTask DisposeAsync()
        {
            if (DataReference is not null)
                await DataReference.DisposeAsync();

            DataReference = null;

            //GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            TradingService.Dispose();
            //GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        private async Task GetSports()
        {
            var clientSports = new List<SportSettingSport>();
            if (string.IsNullOrEmpty(_clientCode))
            {
                _clientSportsSettingsModel = new List<SportSettingSport>();
                return;
            }

            var clientSegmentResponse = await TradingService.GetClientSegmentOrderList(new ClientSegmentOrderListRequest
            {
                ClientCode = _clientCode
            });

            var clientSegmentOrders = new List<ClientSegmentSortOrderModel>();
            if (clientSegmentResponse.StatusCode == StatusCode.Success)
            {
                clientSegmentOrders = clientSegmentResponse.Result
                    .Select(s => new ClientSegmentSortOrderModel
                    {
                        ClientCode = s.ClientCode,
                        ClientId = s.ClientId,
                        ClientName = s.ClientName,
                        SportId = s.SportId,
                        SportName = s.SportName,
                        SegmentId = s.SegmentId,
                        SegmentName = s.SegmentName.TrimEnd(),
                        SortOrder = s.SortOrder
                    }).ToList();
            }

            var clientLeagueResponse = await TradingService.GetClientLeagueOrderList(new ClientLeagueOrderListRequest
            {
                ClientCode = _clientCode
            });

            var clientLeagueOrders = new List<ClientLeagueSortOrderModel>();
            if (clientLeagueResponse.StatusCode == StatusCode.Success)
            {
                clientLeagueOrders = clientLeagueResponse.Result
                    .Select(s => new ClientLeagueSortOrderModel
                    {
                        ClientCode = s.ClientCode,
                        ClientId = s.ClientId,
                        ClientName = s.ClientName,
                        SportId = s.SportId,
                        SportName = s.SportName.TrimEnd(),
                        SegmentId = s.SegmentId,
                        SegmentName = s.SegmentName.TrimEnd(),
                        LeagueId = s.LeagueId,
                        LeagueName = s.LeagueName.TrimEnd(),
                        SortOrder = s.SortOrder
                    }).ToList();
            }

            var clientSportsOrderResponse = await TradingService.GetClientSportOrderList(new ClientSportOrderListRequest
            {
                ClientCode = _clientCode
            });

            await Task.WhenAll(LeagueLookupTask, SegmentLookupTask);

            var leagueLookupResponse = LeagueLookupTask.Result.Result;
            var segmentLookupResponse = SegmentLookupTask.Result.Result;

            if (clientSportsOrderResponse.StatusCode == StatusCode.Success)
            {
                var leagues = leagueLookupResponse.LeagueLookup.GroupJoin(clientLeagueOrders,
                        le => new { le.SportId, le.SegmentId, le.LeagueId },
                        so => new { so.SportId, so.SegmentId, so.LeagueId },
                        (le, so) => new { Lookup = le, LookupOrder = so })
                        .SelectMany(
                        temp => temp.LookupOrder.DefaultIfEmpty(),
                        (temp, s) => new SportSettingLeague
                        {
                            SportId = temp.Lookup.SportId,
                            SegmentId = temp.Lookup.SegmentId,
                            LeagueId = temp.Lookup.LeagueId,
                            LeagueName = temp.Lookup.LeagueName.TrimEnd(),
                            IsActive = temp.LookupOrder.FirstOrDefault() != null ? true : false,
                            SortOrder = temp.LookupOrder.FirstOrDefault() != null ? temp.LookupOrder.FirstOrDefault().SortOrder : 1000
                        })
                        .OrderBy(o => o.SortOrder)
                        .ToList();

                var segments = segmentLookupResponse.SegmentLookup.GroupJoin(clientSegmentOrders,
                    seg => new { seg.SportId, seg.SegmentId },
                    lp => new { lp.SportId, lp.SegmentId },
                    (seg, lp) => new { Lookup = seg, LookupOrder = lp })
                    .SelectMany(
                    temp => temp.LookupOrder.DefaultIfEmpty(),
                    (temp, s) => new SportSettingSegment
                    {
                        SportId = temp.Lookup.SportId,
                        SegmentId = temp.Lookup.SegmentId,
                        SegmentName = temp.Lookup.SegmentName.TrimEnd(),
                        IsActive = temp.LookupOrder.FirstOrDefault() != null ? true : false,
                        SortOrder = temp.LookupOrder.FirstOrDefault() != null ? temp.LookupOrder.FirstOrDefault().SortOrder : 1000,
                        IsOpen = false,
                        Leagues = leagues.Where(c => c.SportId == temp.Lookup.SportId && c.SegmentId == temp.Lookup.SegmentId).ToList()
                    })
                    .OrderBy(o => o.SortOrder)
                    .ToList();

                _clientSportsSettingsModel = clientSportsOrderResponse.Result
                    .Select(sp => new SportSettingSport
                    {
                        SportId = sp.SportId,
                        SportName = sp.SportName.TrimEnd(),
                        SortOrder = sp.SortOrder,
                        IsActive = sp.IsActive,
                        IsOpen = false,
                        Segments = segments.Where(c => c.SportId == sp.SportId).ToList()
                    })
                    .OrderBy(o => o.SortOrder)
                    .ToList();
            }

            _modifiedClientSportsSettingsModel = _clientSportsSettingsModel;
        }

        private async Task InitializeSortTable(string rowSelector, string categorySelector)
        {
            await JSRuntime.InvokeVoidAsync("initSortItems", $".{_tableClass}", rowSelector, ".drag-handle", categorySelector);
        }

        private List<SportsSortOrder> GetSportSortOrder()
        {
            var result = _modifiedClientSportsSettingsModel
                .Select((s, spIndex) => new SportsSortOrder
                {
                    SportId = s.SportId,
                    SportName = s.SportName,
                    SortOrder = spIndex + 1,
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

        #endregion
    }
}
