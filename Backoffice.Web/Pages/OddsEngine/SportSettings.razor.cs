using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.OddsEngine.SportSettings;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text;
using System.Text.Json;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class SportSettings : IAsyncDisposable, IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        public IJSStreamReference? DataReference { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [Parameter] public string Region { get; set; } = "hk";

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public string TableClass { get; set; }
        public string ClientCode { get; set; }

        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public ClientSportsSettingsModel ModifiedClientSportsSettingsModel { get; set; }
        public ClientSportsSettingsModel ClientSportsSettingsModel { get; set; }
        public List<ClientSegmentSortOrderModel> ClientSegmentOrders { get; set; }
        public List<ClientLeagueSortOrderModel> ClientLeagueOrders { get; set; }
        public SegmentLookupResponse SegmentLookupResponse { get; set; }
        public LeagueLookupResponse LeagueLookupResponse { get; set; }

        public bool IsUpdate { get; set; }
        public bool IsPreload { get; set; }

        public string AlertMessage { get; set; }
        public AlertStatus AlertStatus { get; set; }

        #endregion

        #region Fields

        public static Func<int, int, Task> OnSegmentActionAsync;
        public static Func<string, int, Task> OnUpdateSortOrderActionAsync;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            TradingService.Region = Region.ToUpper();
            PageTitle = "Sport Settings";
            IconClass = string.Empty;
            TableClass = "sport-setting-table";

            ClientSportsSettingsModel = new ClientSportsSettingsModel();
            ModifiedClientSportsSettingsModel = new ClientSportsSettingsModel();

            ClientSegmentOrders = new List<ClientSegmentSortOrderModel>();
            ClientLeagueOrders = new List<ClientLeagueSortOrderModel>();

            OnSegmentActionAsync = OnSegmentClickEvent;
            OnUpdateSortOrderActionAsync = OnUpdateSortOrder;

            Breadcrumbs = new List<BreadcrumbModel>();

            Breadcrumbs.Add(new BreadcrumbModel
            {
                Name = "Sport Settings",
                Url = $"{Region}/odds-engine/sport-settings",
                IsCurrentPage = true
            });

            //ClientInformations = (await TradingService.GetClientList()).Result;

            SegmentLookupResponse = (await TradingService.GetSegmentLookup(0)).Result;
            LeagueLookupResponse = (await TradingService.GetLeagueLookup(0, 0)).Result;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            AlertMessage = string.Empty;
        }

        public async Task OnClientChangedEvent(string clientCode)
        {
            ClientCode = clientCode;

            IsPreload = true;

            await GetSports();

            IsPreload = false;
        }

        public async Task OnUpdateClickEvent()
        {
            IsUpdate = true;

            await JSRuntime.InvokeVoidAsync("unloadItems", $".{TableClass}", ".segment-row");
            await JSRuntime.InvokeVoidAsync("unloadItems", $".{TableClass}", ".league-row");

            await InitializeSortTable("> tr.sport-row", ".sport-row");
            await JSRuntime.InvokeVoidAsync("enableSort", $".{TableClass}");
        }

        public async Task OnSaveClickEvent()
        {
            IsPreload = true;
            IsUpdate = false;

            var request = new ClientSportSettingRequest
            {
                ClientCode = ClientCode,
                UserId = UserModel.Id,
                UserName = UserModel.UserName,
                TimeStamp = DateTime.UtcNow,
                SportsSortOrder = GetSportSortOrder(),
            };

            var response = await TradingService.AddOrUpdateClientSportSettings(request);

            if (response.StatusCode == StatusCode.Success)
            {
                ClientSportsSettingsModel = new ClientSportsSettingsModel();

                Thread.Sleep(1000);

                ClientSportsSettingsModel.Sports = ModifiedClientSportsSettingsModel.Sports
                .Select((s, spIndex) => new ClientSportsSettingsModel.Sport
                {
                    SportId = s.SportId,
                    SportName = s.SportName,
                    SortOrder = spIndex + 1,
                    Segments = s.Segments.Select((se, seIndex) => new ClientSportsSettingsModel.Segment
                    {
                        SportId = s.SportId,
                        SegmentId = se.SegmentId,
                        SegmentName = se.SegmentName,
                        Leagues = se.Leagues.Select((le, leIndex) => new ClientSportsSettingsModel.League
                        {
                            SportId = s.SportId,
                            SegmentId = se.SegmentId,
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

                AlertMessage = "Successfully saved Sport - Segment - League ordering.";
                AlertStatus = AlertStatus.Success;
            }
            else
            {
                AlertMessage = response.Message;
                AlertStatus = AlertStatus.Error;
            }


            IsPreload = false;
        }

        public async Task OnCancelClickEvent()
        {
            IsUpdate = false;

            await JSRuntime.InvokeVoidAsync("unloadItems", $".{TableClass}", ".segment-row");
            await JSRuntime.InvokeVoidAsync("unloadItems", $".{TableClass}", ".league-row");

            await JSRuntime.InvokeVoidAsync("destroySort", $".{TableClass}");

            IsPreload = true;

            StateHasChanged();

            await GetSports();

            IsPreload = false;
        }

        public async Task OnCopyClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("FromClientCode", ClientCode);

            var sports = ClientSportsSettingsModel.Sports
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

            //var client = ClientInformations.FirstOrDefault(c => c.ClientCode == ClientCode);

            ModalService.Open<CopyModal>($"Copy Sports Settings", parameters, options);
        }

        public async Task OnViewLogsClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("ClientCode", ClientCode);

            parameters.Add("DialogStyle", "max-width: 1300px !important;");
            parameters.Add("ModalClass", "show logs-modal");
            parameters.Add("Region", Region);

            var options = new ModalOptions();

            ModalService.Open<ViewLogsModal>("View Logs", parameters, options);
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
            await JSRuntime.InvokeVoidAsync("unloadItems", $".{TableClass}", ".segment-row");
            await JSRuntime.InvokeVoidAsync("unloadItems", $".{TableClass}", ".league-row");

            // verify if there are any open rows for segment
            // if there is open then close it.
            if (ClientSportsSettingsModel.Sports.Any(c => c.SportId != model.SportId && c.IsOpen))
            {
                var sport = ClientSportsSettingsModel.Sports.FirstOrDefault(c => c.SportId != model.SportId && c.IsOpen);
                sport.IsOpen = false;
            }

            if (IsUpdate)
            {
                await JSRuntime.InvokeVoidAsync("destroySort", $".{TableClass}");

                if (model.IsOpen)
                {
                    model.IsOpen = false;

                    await JSRuntime.InvokeVoidAsync("collapseButton", ".sport-row");
                    await JSRuntime.InvokeVoidAsync("unloadItems", $".{TableClass}", ".segment-row");

                    await InitializeSortTable("> tr.sport-row", ".sport-row");
                }
                else
                {
                    model.IsOpen = true;

                    // fetch the records on ModifiedClientSportsSettingsModel since this is the record that's being modified
                    var modSport = ModifiedClientSportsSettingsModel.Sports.FirstOrDefault(c => c.SportId == model.SportId);
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
                    await JSRuntime.InvokeVoidAsync("unloadItems", $".{TableClass}", ".segment-row");
                }
                else
                {
                    model.IsOpen = true;

                    var modSport = ClientSportsSettingsModel.Sports.FirstOrDefault(c => c.SportId == model.SportId);
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
            AlertMessage = "Sport settings copied successfully.";
            AlertStatus = AlertStatus.Success;
        }

        public async Task OnSegmentClickEvent(int sportId, int segmentId)
        {
            await JSRuntime.InvokeVoidAsync("unloadItems", $".{TableClass}", ".league-row");

            var sport = ClientSportsSettingsModel.Sports.FirstOrDefault(c => c.SportId == sportId);
            var model = sport.Segments.FirstOrDefault(c => c.SegmentId == segmentId);

            // verify if there are any open rows for leagues
            // if there is open then close it.
            if (sport.Segments.Any(c => c.SportId != model.SportId && c.SegmentId != model.SegmentId && c.IsOpen))
            {
                var segment = sport.Segments.FirstOrDefault(c => c.SportId != model.SportId && c.SegmentId != model.SegmentId && c.IsOpen);
                segment.IsOpen = false;
            }

            if (IsUpdate)
            {
                await JSRuntime.InvokeVoidAsync("destroySort", $".{TableClass}");

                if (model.IsOpen)
                {
                    model.IsOpen = false;

                    await JSRuntime.InvokeVoidAsync("collapseButton", ".segment-row");
                    await JSRuntime.InvokeVoidAsync("unloadItems", $".{TableClass}", ".league-row");

                    await InitializeSortTable("> tr.segment-row", ".segment-row");
                }
                else
                {
                    model.IsOpen = true;

                    // fetch the records on ModifiedClientSportsSettingsModel since this is the record that's being modified
                    var modSport = ModifiedClientSportsSettingsModel.Sports.FirstOrDefault(c => c.SportId == model.SportId);
                    var modSegment = modSport.Segments.FirstOrDefault(c => c.SegmentId == model.SegmentId);

                    var leagues = modSegment.Leagues.Select((s, index) => new
                    {
                        Id = $"league-{s.SportId}-{s.SegmentId}-{s.LeagueId}",
                        LeagueName = s.LeagueName,
                        SortOrder = s.SortOrder,
                        RowClass = "league-row"
                    }).ToList();

                    await JSRuntime.InvokeVoidAsync("loadLeagueItems", $"#segment-{model.SportId}-{model.SegmentId}", leagues);
                    await InitializeSortTable("> tr.league-row", ".league-row");
                }
            }
            else
            {
                if (model.IsOpen)
                {
                    model.IsOpen = false;

                    await JSRuntime.InvokeVoidAsync("collapseButton", ".segment-row");
                    await JSRuntime.InvokeVoidAsync("unloadItems", $".{TableClass}", ".league-row");
                }
                else
                {
                    model.IsOpen = true;

                    var modSport = ClientSportsSettingsModel.Sports.FirstOrDefault(c => c.SportId == model.SportId);
                    var modSegment = modSport.Segments.FirstOrDefault(c => c.SegmentId == model.SegmentId);

                    var leagues = modSegment.Leagues.Select((s, index) => new
                    {
                        Id = $"league-{s.SportId}-{s.SegmentId}-{s.LeagueId}",
                        LeagueName = s.LeagueName,
                        SortOrder = s.SortOrder,
                        RowClass = "league-row"
                    }).ToList();

                    await JSRuntime.InvokeVoidAsync("loadLeagueItems", $"#segment-{model.SportId}-{model.SegmentId}", leagues);
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
                var sport = ModifiedClientSportsSettingsModel.Sports.FirstOrDefault(c => c.SportId == sportId);

                ModifiedClientSportsSettingsModel.Sports.Remove(sport);
                ModifiedClientSportsSettingsModel.Sports.Insert(index, sport);
            }
            else if (splitId[0].ToLower() == "segment")
            {
                var sportId = int.Parse(splitId[1]);
                var sport = ModifiedClientSportsSettingsModel.Sports.FirstOrDefault(c => c.SportId == sportId);

                var segmentId = int.Parse(splitId[2]);
                var segment = sport.Segments.FirstOrDefault(c => c.SegmentId == segmentId);

                sport.Segments.Remove(segment);
                sport.Segments.Insert(index, segment);
            }
            else if (splitId[0].ToLower() == "league")
            {
                var sportId = int.Parse(splitId[1]);
                var sport = ModifiedClientSportsSettingsModel.Sports.FirstOrDefault(c => c.SportId == sportId);

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
            if (string.IsNullOrEmpty(ClientCode))
            {
                ClientSportsSettingsModel.Sports = new List<ClientSportsSettingsModel.Sport>();
                return;
            }

            var clientSegmentResponse = await TradingService.GetClientSegmentOrderList(new ClientSegmentOrderListRequest
            {
                ClientCode = ClientCode
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
                        SegmentName = s.SegmentName.TrimEnd(),
                        SortOrder = s.SortOrder
                    }).ToList();
            }

            var clientLeagueResponse = await TradingService.GetClientLeagueOrderList(new ClientLeagueOrderListRequest
            {
                ClientCode = ClientCode
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
                ClientCode = ClientCode
            });

            if (clientSportsOrderResponse.StatusCode == StatusCode.Success)
            {

                var leagues = LeagueLookupResponse.LeagueLookup.GroupJoin(ClientLeagueOrders,
                        le => new { le.SportId, le.SegmentId, le.LeagueId },
                        so => new { so.SportId, so.SegmentId, so.LeagueId },
                        (le, so) => new { Lookup = le, LookupOrder = so })
                        .SelectMany(
                        temp => temp.LookupOrder.DefaultIfEmpty(),
                        (temp, s) => new ClientSportsSettingsModel.League
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

                var segments = SegmentLookupResponse.SegmentLookup.GroupJoin(ClientSegmentOrders,
                    seg => new { seg.SportId, seg.SegmentId },
                    lp => new { lp.SportId, lp.SegmentId },
                    (seg, lp) => new { Lookup = seg, LookupOrder = lp })
                    .SelectMany(
                    temp => temp.LookupOrder.DefaultIfEmpty(),
                    (temp, s) => new ClientSportsSettingsModel.Segment
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

                ClientSportsSettingsModel.Sports = clientSportsOrderResponse.Result
                    .Select(sp => new ClientSportsSettingsModel.Sport
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

            ModifiedClientSportsSettingsModel = ClientSportsSettingsModel;            
        }

        private async Task InitializeSortTable(string rowSelector, string categorySelector)
        {
            await JSRuntime.InvokeVoidAsync("initSortItems", $".{TableClass}", rowSelector, ".drag-handle", categorySelector);
        }

        private List<SportsSortOrder> GetSportSortOrder()
        {
            var result = ModifiedClientSportsSettingsModel.Sports
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
