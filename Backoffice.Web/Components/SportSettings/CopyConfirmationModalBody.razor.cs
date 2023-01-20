using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using static Backoffice.Shared.Models.Response.LeagueLookupResponse;

namespace Backoffice.Web.Components.SportSettings
{
    public partial class CopyConfirmationModalBody : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        //[Inject] public IMessageBroker MessageBroker { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        protected bool IsPreLoader { get; set; } = false;
        public string CopyFromClientName { get; set; }
        public string CopyFromClientCode { get; set; }
        public string CopyToClientCode { get; set; }
        public string CopyToClientName { get; set; }
        public ElementReference CopyButton { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            CopyFromClientCode = ModalParameters.TryGet<string>("CopyClientCode");
            CopyFromClientName = ModalParameters.TryGet<string>("CopyClientName");
            CopyToClientCode = ModalParameters.TryGet<string>("CopyToClientCode");
            CopyToClientName = ModalParameters.TryGet<string>("CopyToClientName");

            IsPreLoader = false;
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        private async Task<List<SportsSortOrder>> GetContent(string clientCode)
        {
            Console.WriteLine("Get Content");
            if (string.IsNullOrEmpty(clientCode))
            {
                return new List<SportsSortOrder>();
            }

            IsPreLoader = true;
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
            return clientSports.OrderBy(o => o.SortOrder).ToList();
        }
        protected async Task OnCopySportOrder()
        {
            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);
            var request = new ClientSportSettingRequest
            {
                ClientCode = CopyToClientCode,
                UserId = UserModel.Id,
                UserName = UserModel.UserName,
                TimeStamp = DateTime.UtcNow,
                SportsSortOrder = await GetContent(CopyFromClientCode),
            };

            var response = (await TradingService.AddOrUpdateClientSportSettings(request)).Result;

            if (response.StatusCode == StatusCode.Success)
            {
                //await JsRuntime.InvokeVoidAsync("ShowNotication", CopyButton, $"{CopyClientName} Sports Settings is successfully copied to {CopyToClientName}");

                await InvokeAsync(StateHasChanged);

                //await InitializeCallback(CopyToClientCode);
            }

            var tempSuccesMessage = $"{CopyFromClientName} Sports Settings is successfully copied to {CopyToClientName}";
            var tempFailMessage = $"Copy Sport Settings for {CopyToClientName} from {CopyFromClientName} Failed!";
            //var AlertMessages = response.StatusCode == StatusCode.Success ? $"{CopyClientName} Sports Settings is successfully copied to {CopyToClientName}" : $"Copy Sport Settings for {CopyToClientName} from {CopyToClientName} Failed!";

            IsPreLoader = false;
            ModalService.Cancel();
        }

        public void Dispose()
        {
        }

        #endregion
        #region Utility
        private async Task<List<SelectListItemModel<int>>> GetSports()
        {
            var result = (await TradingService.GetSportLookup()).Result;

            if (result == null)
                return new List<SelectListItemModel<int>>();

            var sports = result.SportLookup?.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SportId,
                Name = s.SportName
            }).ToList();

            return sports;
        }
        #endregion
    }
}