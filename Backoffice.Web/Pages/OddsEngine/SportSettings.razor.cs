using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.OddsEngine.SportSettings;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Runtime.Caching;
using System.Security.Claims;
using static Backoffice.Shared.Models.Response.LeagueLookupResponse;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class SportSettings : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        private bool IsPreLoader { get; set; }

        public ClientSportOrderFilterModel ClientSportOrderFilter { get; set; }
        private List<SportsSortOrder> ClientSportOrderList { get; set; }
        private bool IsSelectAll { get; set; } = false;
        private int currentIndex { get; set; }
        private int currentSportId { get; set; }
        private int currentSegmentIndex { get; set; }
        private int currentLeagueIndex { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Sport Settings";
            IconClass = string.Empty;

            ClientSportOrderList = new List<SportsSortOrder>();
            ClientSportOrderFilter = new ClientSportOrderFilterModel();
        }
        public async Task InitializeCallback(string clientCode)
        {
            ClientSportOrderFilter.IsUpdate = false;
            await GetContent(clientCode);
        }

        public async Task OnClientChangedEvent()
        {
            IsPreLoader = true;
            await GetContent(ClientSportOrderFilter.ClientCode);
        }

        public void Dispose()
        {
        }
        #endregion

        private async Task GetContent(string clientCode)
        {
            Console.WriteLine("Get Content");
            if (string.IsNullOrEmpty(clientCode))
            {
                ClientSportOrderList = new List<SportsSortOrder>();
                return;
            }

            IsPreLoader = true;
            ClientSportOrderList = null;
            await InvokeAsync(StateHasChanged);

            var clientSports = new List<SportsSortOrder>();

            var sportOrder = (await TradingService.GetClientSportOrderList(new ClientSportOrderListRequest { ClientCode = clientCode })).Result;

            var result = sportOrder ?? new List<ClientSportOrderListResponse>();

            if (result.Any())
            {
                clientSports = result.Select(s => new SportsSortOrder
                {
                    SportId = s.SportId,
                    SportName = s.SportName,
                    SortOrder = s.SortOrder,
                    IsActive = s.IsActive,
                    IsOpen = false,
                }).ToList();
            }

            var sports = await GetSports();
            if (sports != null && sports.Any())
            {
                //temporarily add virtual sports since it is not handled in API
                sports.Add(new SelectListItemModel<int> { Value = 1001, Name = "Virtual Soccer" });
                sports.Add(new SelectListItemModel<int> { Value = 1002, Name = "Virtual Basketball" });
                sports.Add(new SelectListItemModel<int> { Value = 1003, Name = "Virtual Baseball" });
                sports.Add(new SelectListItemModel<int> { Value = 1005, Name = "Virtual Tennis" });
                sports.Add(new SelectListItemModel<int> { Value = 1021, Name = "Virtual Cricket" });
                sports.Add(new SelectListItemModel<int> { Value = 1055, Name = "Virtual Horse Racing" });
                sports.Add(new SelectListItemModel<int> { Value = 1058, Name = "Virtual Dog Racing" });

                var sortOrder = clientSports.Count();
                var newSports = new List<SportsSortOrder>();

                if (clientSports != null && clientSports.Any())
                {
                    var existingSportIds = clientSports.Select(s => s.SportId).ToList() ?? new List<int>();
                    var tempSports = sports.Where(c => !existingSportIds.Contains(c.Value));
                    if (tempSports != null && tempSports.Any())
                    {
                        foreach (var sport in tempSports)
                        {
                            sortOrder++;
                            newSports.Add(new SportsSortOrder
                            {
                                SportId = sport.Value,
                                SportName = sport.Name,
                                SortOrder = sortOrder,
                                IsActive = false,
                                IsOpen = false,
                            });
                        }
                    }
                }
                else
                {
                    foreach (var sport in sports)
                    {
                        sortOrder++;
                        newSports.Add(new SportsSortOrder
                        {
                            SportId = sport.Value,
                            SportName = sport.Name,
                            SortOrder = sortOrder,
                            IsActive = false,
                            IsOpen = false,
                        });
                    }
                }

                if (newSports.Any())
                    clientSports.AddRange(newSports);
            }

            //get segments
            if (clientSports.Any())
            {
                var clientSegment = (await TradingService.GetClientSegmentOrderList(new ClientSegmentOrderListRequest { ClientCode = clientCode })).Result;
                var clientLeagues = (await TradingService.GetClientLeagueOrderList(new ClientLeagueOrderListRequest { ClientCode = clientCode })).Result;
                var clientSegmentResult = clientSegment ?? new List<ClientSegmentOrderListResponse>();
                var clientLeagueResult = clientLeagues ?? new List<ClientLeagueOrderListResponse>();

                var segmentLookup = (await TradingService.GetSegmentLookup(0)).Result ?? new SegmentLookupResponse();
                var leagueLookup = (await TradingService.GetLeagueLookup(0, 0)).Result ?? new LeagueLookupResponse();

                var sportIds = clientSports.Select(s => s.SportId).Distinct().ToList();
                var tierLeagueSortOrder = (await TradingService.GetTierLeagueOrder()).Result ?? new List<TierLeagueOrderModel>();

                foreach (var sport in clientSports)
                {
                    var segmentResult = clientSegmentResult.Where(s => s.SportId == sport.SportId).ToList();
                    var segments = segmentLookup?.SegmentLookup?.Where(s => s.SportId == sport.SportId) ?? new List<SegmentLookupModel>();
                    if (segments.Any())
                    {
                        sport.SegmentsSortOrder = segments.Select(s => new SportsSegmentSortOrder
                        {
                            SportId = sport.SportId,
                            SportName = sport.SportName,
                            SegmentId = s.SegmentId,
                            SegmentName = s.SegmentName,
                            IsActive = segmentResult?.Any(c => c.SegmentId == s.SegmentId) ?? false,
                            SortOrder = segmentResult.FirstOrDefault(c => c.SegmentId == s.SegmentId)?.SortOrder ?? 1000,
                            IsOpen = false,
                        }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ToList();


                        //get leagues
                        var segmentOrder = 0;
                        sport.SegmentsSortOrder.ForEach(c =>
                        {
                            segmentOrder++;
                            c.SortOrder = segmentOrder;
                            var leagueResult = clientLeagueResult.Where(s => s.SportId == c.SportId && s.SegmentId == c.SegmentId).ToList();

                            var leagues = leagueLookup?.LeagueLookup?.Where(s => s.SportId == sport.SportId && s.SegmentId == c.SegmentId) ?? new List<LeagueLookupModel>();
                            if (leagues.Any())
                            {
                                c.LeagueSortOrder = leagues.Select(l => new SportSegmentLeagueSortOrder
                                {
                                    SportId = sport.SportId,
                                    SportName = sport.SportName,
                                    SegmentId = c.SegmentId,
                                    SegmentName = c.SegmentName,
                                    LeagueId = l.LeagueId,
                                    LeagueName = l.LeagueName,
                                    IsActive = leagueResult?.Any(c => c.LeagueId == l.LeagueId) ?? false,
                                    SortOrder = leagueResult.FirstOrDefault(c => c.LeagueId == l.LeagueId)?.SortOrder ??
                                        tierLeagueSortOrder.FirstOrDefault(t => t.SportId == l.SportId && t.SegmentId == l.SegmentId && t.LeagueId == l.LeagueId)?.SortOrder + 1000 ?? 2000
                                }).OrderBy(o => o.SortOrder).ThenBy(o => o.LeagueName).ToList();

                                var leagueOrder = 0;
                                c.LeagueSortOrder.ForEach(ls =>
                                {
                                    leagueOrder++;
                                    ls.SortOrder = leagueOrder;
                                });
                            }
                            else
                            {
                                c.LeagueSortOrder = new List<SportSegmentLeagueSortOrder>();
                            }
                        });
                    }
                    else
                    {
                        sport.SegmentsSortOrder = new List<SportsSegmentSortOrder>();
                    }
                }
            }

            IsSelectAll = !clientSports.Any(s => s.IsActive == false);
            ClientSportOrderList = clientSports.OrderBy(o => o.SortOrder).ToList();
            IsPreLoader = false;

            await InvokeAsync(StateHasChanged);
        }

        #region Events
        protected async Task CheckboxClicked(CheckBoxModel model)
        {
            IsSelectAll = model.Checked;
            if (ClientSportOrderList.Any())
            {
                Parallel.ForEach(ClientSportOrderList, sport =>
                {
                    sport.IsActive = model.Checked;
                });
            }
            await InvokeAsync(StateHasChanged);
        }
        protected async Task CheckboxSportClicked(CheckBoxModel model)
        {
            ((SportsSortOrder)model.Param1).IsActive = model.Checked;

            IsSelectAll = !ClientSportOrderList.Any(s => s.IsActive == false);
        }
        protected async Task OnCopyClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ClientSportOrderFilter", ClientSportOrderFilter);
            parameters.Add("DialogStyle", "max-width: 600px;");

            var options = new ModalOptions();
            ModalService.Open<SportSettingsCopyModal>($"Copy {ClientSportOrderFilter.ClientName} Sports Settings", parameters, options);
            await InvokeAsync(StateHasChanged);
        }
        public async Task OnSaveClickEvent()
        {
            ClientSportOrderFilter.IsUpdate = false;
            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);

            //int sortOrder = 0;
            //var newSportOrder = new List<SportsSortOrder>();
            //foreach (var sport in ClientSportOrderList)
            //{
            //    sortOrder++;
            //    var sportOrder = new SportsSortOrder()
            //    {
            //        SportId = sport.SportId,
            //        SportName = sport.SportName,
            //        SortOrder = sortOrder,
            //        IsActive = sport.IsActive,
            //        SegmentsSortOrder = new List<SportsSegmentSortOrder>()
            //    };

            //    int segmentOrderCtr = 0;
            //    var segmentOrders = new List<SportsSegmentSortOrder>();
            //    var segments = sport.SegmentsSortOrder.ToList();
            //    if (segments.Any())
            //    {
            //        foreach (var segment in segments)
            //        {
            //            segmentOrderCtr++;
            //            var segmentOrder = new SportsSegmentSortOrder
            //            {
            //                SportId = segment.SportId,
            //                SportName = segment.SportName,
            //                SegmentId = segment.SegmentId,
            //                SegmentName = segment.SegmentName,
            //                SortOrder = segmentOrderCtr,
            //                IsActive = segment.IsActive,
            //            };

            //            int leagueOrderCtr = 0;
            //            var leagueOrders = new List<SportSegmentLeagueSortOrder>();
            //            var leagues = segment.LeagueSortOrder.ToList();
            //            if (leagues.Any())
            //            {
            //                foreach (var league in leagues)
            //                {
            //                    leagueOrderCtr++;
            //                    leagueOrders.Add(new SportSegmentLeagueSortOrder
            //                    {
            //                        SportId = league.SportId,
            //                        SportName = league.SportName,
            //                        SegmentId = league.SegmentId,
            //                        SegmentName = league.SegmentName,
            //                        LeagueId = league.LeagueId,
            //                        LeagueName = league.LeagueName,
            //                        SortOrder = leagueOrderCtr,
            //                        IsActive = league.IsActive,
            //                    });
            //                }
            //            }
            //            segmentOrder.LeagueSortOrder = leagueOrders;

            //            segmentOrders.Add(segmentOrder);
            //        }
            //        sportOrder.SegmentsSortOrder = segmentOrders;
            //    }

            //    newSportOrder.Add(sportOrder);
            //}

            var request = new ClientSportSettingRequest
            {
                ClientCode = ClientSportOrderFilter.ClientCode,
                UserId = UserModel.Id,
                UserName = UserModel.UserName,
                TimeStamp = DateTime.UtcNow,
                SportsSortOrder = ClientSportOrderList,
            };

            var response = await TradingService.AddOrUpdateClientSportSettings(request);
            IsPreLoader = false;
            await InvokeAsync(StateHasChanged);

            var AlertMessages = response?.Result.StatusCode == StatusCode.Success ? $"Sport Settings of {ClientSportOrderFilter.ClientCode} has been successfully updated." : $"Update Sport Settings for {ClientSportOrderFilter.ClientCode} Failed!";

        }
        protected async Task OnViewLogsEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ClientCode", ClientSportOrderFilter.ClientCode);
            parameters.Add("DialogStyle", "max-width: 1300px !important;");
            parameters.Add("ModalClass", "show logs-modal");

            var options = new ModalOptions();
            ModalService.Open<ViewLogsModalBody>("View Logs", parameters, options);
        }
        public void Drop(SportsSortOrder item)
        {
            if (item != null)
            {
                var index = GetIndex(item.SportId);
                // get current item
                var current = ClientSportOrderList[currentIndex];
                // remove game from current index
                ClientSportOrderList.RemoveAt(currentIndex);
                ClientSportOrderList.Insert(index, current);

                // update current selection
                currentIndex = index;

                StateHasChanged();
            }
        }
        public void Drop(SportsSegmentSortOrder item)
        {
            if (item != null && item.SportId == currentSportId)
            {
                var sport = ClientSportOrderList.FirstOrDefault(s => s.SportId == item.SportId);
                if (sport != null)
                {
                    var index = GetIndex(item.SportId, item.SegmentId);
                    // get current item
                    var current = sport.SegmentsSortOrder[currentSegmentIndex];
                    // remove game from current index
                    sport.SegmentsSortOrder.RemoveAt(currentSegmentIndex);
                    sport.SegmentsSortOrder.Insert(index, current);

                    // update current selection
                    currentSegmentIndex = index;

                    StateHasChanged();
                }
            }
        }
        public void Drop(SportSegmentLeagueSortOrder item)
        {
            if (item != null && item.SportId == currentSportId)
            {
                var sport = ClientSportOrderList.FirstOrDefault(s => s.SportId == item.SportId);
                if (sport != null)
                {
                    var segment = sport.SegmentsSortOrder.FirstOrDefault(s => s.SegmentId == item.SegmentId);
                    if (segment != null)
                    {
                        var index = GetIndex(item);
                        // get current item
                        var current = segment.LeagueSortOrder[currentLeagueIndex];
                        // remove game from current index
                        segment.LeagueSortOrder.RemoveAt(currentLeagueIndex);
                        segment.LeagueSortOrder.Insert(index, current);

                        // update current selection
                        currentLeagueIndex = index;

                        StateHasChanged();
                    }
                }
            }
        }

        #endregion

        #region Utilities
        private async Task<List<SelectListItemModel<int>>> GetSports()
        {
            var result = (await TradingService.GetSportLookup()).Result;

            if (result == null)
                return new List<SelectListItemModel<int>>();

            var sports = result.Sports?.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SportId,
                Name = s.SportName
            }).ToList();

            return sports;
        }
        int GetIndex(int sportId)
        {
            return ClientSportOrderList.FindIndex(a => a.SportId == sportId);
        }
        int GetIndex(int sportId, int segmentId)
        {
            var sport = ClientSportOrderList.FirstOrDefault(a => a.SportId == sportId);
            return sport?.SegmentsSortOrder?.FindIndex(a => a.SportId == sportId && a.SegmentId == segmentId) ?? 0;
        }
        int GetIndex(SportSegmentLeagueSortOrder item)
        {
            var sport = ClientSportOrderList.FirstOrDefault(a => a.SportId == item.SportId);
            var segment = sport?.SegmentsSortOrder?.FirstOrDefault(s => s.SegmentId == item.SegmentId);
            return segment?.LeagueSortOrder?.FindIndex(a => a.SportId == item.SportId && a.SegmentId == item.SegmentId && a.LeagueId == item.LeagueId) ?? 0;
        }
        public void StartDrag(SportsSortOrder item)
        {
            currentIndex = GetIndex(item.SportId);
        }
        public void StartDrag(SportsSegmentSortOrder item)
        {
            currentIndex = GetIndex(item.SportId);
            currentSportId = item.SportId;
            currentSegmentIndex = GetIndex(item.SportId, item.SegmentId);
        }
        public void StartDrag(SportSegmentLeagueSortOrder item)
        {
            currentIndex = GetIndex(item.SportId);
            currentSportId = item.SportId;
            currentSegmentIndex = GetIndex(item.SportId, item.SegmentId);
            currentLeagueIndex = GetIndex(item);
        }
        #endregion
    }
}
