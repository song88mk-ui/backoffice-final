using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class SportSettings : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public string TableClass { get; set; }

        public ClientSportOrderFilterModel ClientSportOrderFilter { get; set; }
        public ClientSportsSettingsModel ClientSportsSettingsModel { get; set; }
        public List<ClientSegmentSortOrderModel> ClientSegmentOrders { get; set; }
        public List<ClientLeagueSortOrderModel> ClientLeagueOrders { get; set; }
        public bool IsUpdate { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Sport Settings";
            IconClass = string.Empty;
            TableClass = "sport-setting-table";

            ClientSportsSettingsModel = new ClientSportsSettingsModel();
            ClientSegmentOrders = new List<ClientSegmentSortOrderModel>();
            ClientLeagueOrders = new List<ClientLeagueSortOrderModel>();
        }

        public async Task OnClientChangedEvent(string clientCode)
        {
            await GetSports(clientCode);
        }

        public async Task OnUpdateClickEvent()
        {
            IsUpdate = true;

            await JSRuntime.InvokeVoidAsync("initSortItems", $".{TableClass}", "> tr.sport-row", ".drag-handle");
            await JSRuntime.InvokeVoidAsync("enableSort", $".{TableClass}");
        }

        public async Task OnCancelClickEvent()
        {
            IsUpdate = false;

            await JSRuntime.InvokeVoidAsync("destroySort", $".{TableClass}");
        }

        public async Task OnActiveHeaderChangedEvent(ChangeEventArgs e)
        {
            var isActive = (bool)e.Value;

            ClientSportsSettingsModel.Sports = ClientSportsSettingsModel.Sports
                .Select(s =>
                {
                    s.IsActive = (bool)e.Value;

                    return s;
                }).ToList();
        }

        public async Task OnSportClickEvent(ClientSportsSettingsModel.Sport model)
        {
            if (!IsUpdate)
                return;

            await JSRuntime.InvokeVoidAsync("destroySort", $".{TableClass}");

            if (model.IsOpen)
            {
                await JSRuntime.InvokeVoidAsync("initSortItems", $".{TableClass}", "> tr.sport-row", ".drag-handle");
                model.IsOpen = false;
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("initSortItems", $".{TableClass}", "> tr.segment-row", ".drag-handle");
                model.IsOpen = true;
            }

            // prepare segments (one time only)
            if (!model.Segments.Any())
            {
                var segmentResponse = await TradingService.GetSegmentLookup(model.SportId);

                if (segmentResponse.StatusCode == StatusCode.Success)
                {
                    model.Segments = segmentResponse.Result.SegmentLookup
                        .GroupJoin(ClientSegmentOrders,
                        se => new { se.SportId, se.SegmentId },
                        so => new { so.SportId, so.SegmentId },
                        (se, so) => new { Lookup = se, LookupOrder = so })
                        .SelectMany(
                        temp => temp.LookupOrder.DefaultIfEmpty(),
                        (temp, s) => new ClientSportsSettingsModel.Segment
                        {
                            SegmentId = temp.Lookup.SegmentId,
                            SegmentName = temp.Lookup.SegmentName,
                            IsActive = temp.LookupOrder.FirstOrDefault() != null ? true : false,
                            SortOrder = temp.LookupOrder.FirstOrDefault() != null ? temp.LookupOrder.FirstOrDefault().SortOrder : 1000,
                            IsOpen = false
                        })
                        .OrderBy(o => o.SortOrder)
                        .ToList();
                }
            }
        }

        public async Task OnSegmentClickEvent(int sportId, ClientSportsSettingsModel.Segment model)
        {
            if (!IsUpdate)
                return;

            await JSRuntime.InvokeVoidAsync("destroySort", $".{TableClass}");

            if (model.IsOpen)
            {
                await JSRuntime.InvokeVoidAsync("initSortItems", $".{TableClass}", "> tr.segment-row", ".drag-handle");
                model.IsOpen = false;
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("initSortItems", $".{TableClass}", "> tr.league-row", ".drag-handle");
                model.IsOpen = true;
            }

            // prepare leagues (one time only)
            if (!model.Leagues.Any())
            {
                var leagueResponse = await TradingService.GetLeagueLookup(sportId, model.SegmentId);

                if (leagueResponse.StatusCode == StatusCode.Success)
                {
                    model.Leagues = leagueResponse.Result.LeagueLookup
                        .GroupJoin(ClientLeagueOrders,
                        le => new { le.SportId, le.SegmentId, le.LeagueId },
                        so => new { so.SportId, so.SegmentId, so.LeagueId },
                        (le, so) => new { Lookup = le, LookupOrder = so })
                        .SelectMany(
                        temp => temp.LookupOrder.DefaultIfEmpty(),
                        (temp, s) => new ClientSportsSettingsModel.League
                        {
                            LeagueId = temp.Lookup.LeagueId,
                            LeagueName = temp.Lookup.LeagueName,
                            IsActive = temp.LookupOrder.FirstOrDefault() != null ? true : false,
                            SortOrder = temp.LookupOrder.FirstOrDefault() != null ? temp.LookupOrder.FirstOrDefault().SortOrder : 1000
                        })
                        .OrderBy(o => o.SortOrder)
                        .ToList();
                }
            }
        }

        //public async Task CheckboxClicked(CheckBoxModel model)
        //{
        //    //IsSelectAll = model.Checked;
        //    //if (ClientSportOrderList.Any())
        //    //{
        //    //    Parallel.ForEach(ClientSportOrderList, sport =>
        //    //    {
        //    //        sport.IsActive = model.Checked;
        //    //    });
        //    //}
        //}

        //protected async Task CheckboxSportClicked(CheckBoxModel model)
        //{
        //    ((SportsSortOrder)model.Param1).IsActive = model.Checked;

        //    IsSelectAll = !ClientSportOrderList.Any(s => s.IsActive == false);
        //}

        //protected async Task OnCopyClickEvent()
        //{
        //    var parameters = new ModalParameters();
        //    parameters.Add("ClientSportOrderFilter", ClientSportOrderFilter);
        //    parameters.Add("DialogStyle", "max-width: 600px;");

        //    var options = new ModalOptions();
        //    ModalService.Open<SportSettingsCopyModal>($"Copy {ClientSportOrderFilter.ClientName} Sports Settings", parameters, options);
        //    await InvokeAsync(StateHasChanged);
        //}

        //public async Task OnSaveClickEvent()
        //{
        //    ClientSportOrderFilter.IsUpdate = false;
        //    IsPreLoader = true;
        //    await InvokeAsync(StateHasChanged);

        //    //int sortOrder = 0;
        //    //var newSportOrder = new List<SportsSortOrder>();
        //    //foreach (var sport in ClientSportOrderList)
        //    //{
        //    //    sortOrder++;
        //    //    var sportOrder = new SportsSortOrder()
        //    //    {
        //    //        SportId = sport.SportId,
        //    //        SportName = sport.SportName,
        //    //        SortOrder = sortOrder,
        //    //        IsActive = sport.IsActive,
        //    //        SegmentsSortOrder = new List<SportsSegmentSortOrder>()
        //    //    };

        //    //    int segmentOrderCtr = 0;
        //    //    var segmentOrders = new List<SportsSegmentSortOrder>();
        //    //    var segments = sport.SegmentsSortOrder.ToList();
        //    //    if (segments.Any())
        //    //    {
        //    //        foreach (var segment in segments)
        //    //        {
        //    //            segmentOrderCtr++;
        //    //            var segmentOrder = new SportsSegmentSortOrder
        //    //            {
        //    //                SportId = segment.SportId,
        //    //                SportName = segment.SportName,
        //    //                SegmentId = segment.SegmentId,
        //    //                SegmentName = segment.SegmentName,
        //    //                SortOrder = segmentOrderCtr,
        //    //                IsActive = segment.IsActive,
        //    //            };

        //    //            int leagueOrderCtr = 0;
        //    //            var leagueOrders = new List<SportSegmentLeagueSortOrder>();
        //    //            var leagues = segment.LeagueSortOrder.ToList();
        //    //            if (leagues.Any())
        //    //            {
        //    //                foreach (var league in leagues)
        //    //                {
        //    //                    leagueOrderCtr++;
        //    //                    leagueOrders.Add(new SportSegmentLeagueSortOrder
        //    //                    {
        //    //                        SportId = league.SportId,
        //    //                        SportName = league.SportName,
        //    //                        SegmentId = league.SegmentId,
        //    //                        SegmentName = league.SegmentName,
        //    //                        LeagueId = league.LeagueId,
        //    //                        LeagueName = league.LeagueName,
        //    //                        SortOrder = leagueOrderCtr,
        //    //                        IsActive = league.IsActive,
        //    //                    });
        //    //                }
        //    //            }
        //    //            segmentOrder.LeagueSortOrder = leagueOrders;

        //    //            segmentOrders.Add(segmentOrder);
        //    //        }
        //    //        sportOrder.SegmentsSortOrder = segmentOrders;
        //    //    }

        //    //    newSportOrder.Add(sportOrder);
        //    //}

        //    var request = new ClientSportSettingRequest
        //    {
        //        ClientCode = ClientSportOrderFilter.ClientCode,
        //        UserId = UserModel.Id,
        //        UserName = UserModel.UserName,
        //        TimeStamp = DateTime.UtcNow,
        //        SportsSortOrder = ClientSportOrderList,
        //    };

        //    var response = await TradingService.AddOrUpdateClientSportSettings(request);
        //    IsPreLoader = false;
        //    await InvokeAsync(StateHasChanged);

        //    var AlertMessages = response?.Result.StatusCode == StatusCode.Success ? $"Sport Settings of {ClientSportOrderFilter.ClientCode} has been successfully updated." : $"Update Sport Settings for {ClientSportOrderFilter.ClientCode} Failed!";

        //}

        //protected async Task OnViewLogsEvent()
        //{
        //    var parameters = new ModalParameters();

        //    parameters.Add("ClientCode", ClientSportOrderFilter.ClientCode);
        //    parameters.Add("DialogStyle", "max-width: 1300px !important;");
        //    parameters.Add("ModalClass", "show logs-modal");

        //    var options = new ModalOptions();

        //    ModalService.Open<ViewLogsModalBody>("View Logs", parameters, options);
        //}

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private async Task GetSports(string clientCode)
        {
            if (string.IsNullOrEmpty(clientCode))
            {
                ClientSportsSettingsModel.Sports = new List<ClientSportsSettingsModel.Sport>();
                return;
            }

            var clientSportsOrderResponse = await TradingService.GetClientSportOrderList(new ClientSportOrderListRequest
            {
                ClientCode = clientCode
            });

            if (clientSportsOrderResponse.StatusCode == StatusCode.Success)
            {
                ClientSportsSettingsModel.Sports = clientSportsOrderResponse.Result
                    .Select(sp => new ClientSportsSettingsModel.Sport
                    {
                        SportId = sp.SportId,
                        SportName = sp.SportName,
                        SortOrder = sp.SortOrder,
                        IsActive = sp.IsActive,
                        IsOpen = false
                    })
                    .OrderBy(o => o.SortOrder)
                    .ToList();
            }

            var clientSegmentResponse = await TradingService.GetClientSegmentOrderList(new ClientSegmentOrderListRequest
            {
                ClientCode = clientCode
            });

            if (clientSegmentResponse.StatusCode == StatusCode.Success)
            {
                ClientSegmentOrders = clientSegmentResponse.Result
                    .Select(s => new ClientSegmentSortOrderModel
                    { 
                        ClientCode = s.ClientCode,
                        ClientId = s.ClientId,
                        ClientName = s.ClientName,
                        SportId= s.SportId,
                        SportName= s.SportName,
                        SegmentId = s.SegmentId,
                        SegmentName = s.SegmentName,
                        SortOrder = s.SortOrder
                    }).ToList();
            }

            var clientLeagueResponse = await TradingService.GetClientLeagueOrderList(new ClientLeagueOrderListRequest
            {
                ClientCode = clientCode
            });

            if (clientLeagueResponse.StatusCode == StatusCode.Success)
            {
                ClientLeagueOrders = clientLeagueResponse.Result
                    .Select(s => new ClientLeagueSortOrderModel
                    {
                        ClientCode = s.ClientCode,
                        ClientId = s.ClientId,
                        ClientName = s.ClientName,
                        SportId = s.SportId,
                        SportName = s.SportName,
                        SegmentId = s.SegmentId,
                        SegmentName = s.SegmentName,
                        LeagueId = s.LeagueId,
                        LeagueName = s.LeagueName,
                        SortOrder = s.SortOrder
                    }).ToList();
            }
        }

        //private async Task GetContent(string clientCode)
        //{
        //    if (string.IsNullOrEmpty(clientCode))
        //    {
        //        ClientSportOrderList = new List<SportsSortOrder>();
        //        return;
        //    }

        //    var clientSports = new List<SportsSortOrder>();

        //    var sportOrder = (await TradingService.GetClientSportOrderList(new ClientSportOrderListRequest { ClientCode = clientCode })).Result;

        //    var result = sportOrder ?? new List<ClientSportOrderListResponse>();

        //    if (result.Any())
        //    {
        //        clientSports = result.Select(s => new SportsSortOrder
        //        {
        //            SportId = s.SportId,
        //            SportName = s.SportName,
        //            SortOrder = s.SortOrder,
        //            IsActive = s.IsActive,
        //            IsOpen = false,
        //        }).ToList();
        //    }

        //    var sports = await GetSports();

        //    if (sports != null && sports.Any())
        //    {
        //        //temporarily add virtual sports since it is not handled in API
        //        sports.Add(new SelectListItemModel<int> { Value = 1001, Name = "Virtual Soccer" });
        //        sports.Add(new SelectListItemModel<int> { Value = 1002, Name = "Virtual Basketball" });
        //        sports.Add(new SelectListItemModel<int> { Value = 1003, Name = "Virtual Baseball" });
        //        sports.Add(new SelectListItemModel<int> { Value = 1005, Name = "Virtual Tennis" });
        //        sports.Add(new SelectListItemModel<int> { Value = 1021, Name = "Virtual Cricket" });
        //        sports.Add(new SelectListItemModel<int> { Value = 1055, Name = "Virtual Horse Racing" });
        //        sports.Add(new SelectListItemModel<int> { Value = 1058, Name = "Virtual Dog Racing" });

        //        var sortOrder = clientSports.Count();
        //        var newSports = new List<SportsSortOrder>();

        //        if (clientSports != null && clientSports.Any())
        //        {
        //            var existingSportIds = clientSports.Select(s => s.SportId).ToList() ?? new List<int>();
        //            var tempSports = sports.Where(c => !existingSportIds.Contains(c.Value));
        //            if (tempSports != null && tempSports.Any())
        //            {
        //                foreach (var sport in tempSports)
        //                {
        //                    sortOrder++;
        //                    newSports.Add(new SportsSortOrder
        //                    {
        //                        SportId = sport.Value,
        //                        SportName = sport.Name,
        //                        SortOrder = sortOrder,
        //                        IsActive = false,
        //                        IsOpen = false,
        //                    });
        //                }
        //            }
        //        }
        //        else
        //        {
        //            foreach (var sport in sports)
        //            {
        //                sortOrder++;
        //                newSports.Add(new SportsSortOrder
        //                {
        //                    SportId = sport.Value,
        //                    SportName = sport.Name,
        //                    SortOrder = sortOrder,
        //                    IsActive = false,
        //                    IsOpen = false,
        //                });
        //            }
        //        }

        //        if (newSports.Any())
        //            clientSports.AddRange(newSports);
        //    }

        //    //get segments
        //    if (clientSports.Any())
        //    {
        //        var clientSegment = (await TradingService.GetClientSegmentOrderList(new ClientSegmentOrderListRequest { ClientCode = clientCode })).Result;
        //        var clientLeagues = (await TradingService.GetClientLeagueOrderList(new ClientLeagueOrderListRequest { ClientCode = clientCode })).Result;
        //        var clientSegmentResult = clientSegment ?? new List<ClientSegmentOrderListResponse>();
        //        var clientLeagueResult = clientLeagues ?? new List<ClientLeagueOrderListResponse>();

        //        var segmentLookup = (await TradingService.GetSegmentLookup(0)).Result ?? new SegmentLookupResponse();
        //        var leagueLookup = (await TradingService.GetLeagueLookup(0, 0)).Result ?? new LeagueLookupResponse();

        //        var sportIds = clientSports.Select(s => s.SportId).Distinct().ToList();
        //        var tierLeagueSortOrder = (await TradingService.GetTierLeagueOrder()).Result ?? new List<TierLeagueOrderModel>();

        //        foreach (var sport in clientSports)
        //        {
        //            var segmentResult = clientSegmentResult.Where(s => s.SportId == sport.SportId).ToList();
        //            var segments = segmentLookup?.SegmentLookup?.Where(s => s.SportId == sport.SportId) ?? new List<SegmentLookupModel>();
        //            if (segments.Any())
        //            {
        //                sport.SegmentsSortOrder = segments.Select(s => new SportsSegmentSortOrder
        //                {
        //                    SportId = sport.SportId,
        //                    SportName = sport.SportName,
        //                    SegmentId = s.SegmentId,
        //                    SegmentName = s.SegmentName,
        //                    IsActive = segmentResult?.Any(c => c.SegmentId == s.SegmentId) ?? false,
        //                    SortOrder = segmentResult.FirstOrDefault(c => c.SegmentId == s.SegmentId)?.SortOrder ?? 1000,
        //                    IsOpen = false,
        //                }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ToList();


        //                //get leagues
        //                var segmentOrder = 0;
        //                sport.SegmentsSortOrder.ForEach(c =>
        //                {
        //                    segmentOrder++;
        //                    c.SortOrder = segmentOrder;
        //                    var leagueResult = clientLeagueResult.Where(s => s.SportId == c.SportId && s.SegmentId == c.SegmentId).ToList();

        //                    var leagues = leagueLookup?.LeagueLookup?.Where(s => s.SportId == sport.SportId && s.SegmentId == c.SegmentId) ?? new List<LeagueLookupModel>();
        //                    if (leagues.Any())
        //                    {
        //                        c.LeagueSortOrder = leagues.Select(l => new SportSegmentLeagueSortOrder
        //                        {
        //                            SportId = sport.SportId,
        //                            SportName = sport.SportName,
        //                            SegmentId = c.SegmentId,
        //                            SegmentName = c.SegmentName,
        //                            LeagueId = l.LeagueId,
        //                            LeagueName = l.LeagueName,
        //                            IsActive = leagueResult?.Any(c => c.LeagueId == l.LeagueId) ?? false,
        //                            SortOrder = leagueResult.FirstOrDefault(c => c.LeagueId == l.LeagueId)?.SortOrder ??
        //                                tierLeagueSortOrder.FirstOrDefault(t => t.SportId == l.SportId && t.SegmentId == l.SegmentId && t.LeagueId == l.LeagueId)?.SortOrder + 1000 ?? 2000
        //                        }).OrderBy(o => o.SortOrder).ThenBy(o => o.LeagueName).ToList();

        //                        var leagueOrder = 0;
        //                        c.LeagueSortOrder.ForEach(ls =>
        //                        {
        //                            leagueOrder++;
        //                            ls.SortOrder = leagueOrder;
        //                        });
        //                    }
        //                    else
        //                    {
        //                        c.LeagueSortOrder = new List<SportSegmentLeagueSortOrder>();
        //                    }
        //                });
        //            }
        //            else
        //            {
        //                sport.SegmentsSortOrder = new List<SportsSegmentSortOrder>();
        //            }
        //        }
        //    }

        //    IsSelectAll = !clientSports.Any(s => s.IsActive == false);
        //    ClientSportOrderList = clientSports.OrderBy(o => o.SortOrder).ToList();
        //    IsPreLoader = false;

        //    await InvokeAsync(StateHasChanged);
        //}

        //private async Task<List<SelectListItemModel<int>>> GetSports()
        //{
        //    var result = (await TradingService.GetSportLookup()).Result;

        //    if (result == null)
        //        return new List<SelectListItemModel<int>>();

        //    var sports = result.SportLookup?.Select(s => new SelectListItemModel<int>()
        //    {
        //        Value = s.SportId,
        //        Name = s.SportName
        //    }).ToList();

        //    return sports;
        //}

        #endregion
    }
}
