using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class CopyConfirmationModalBody : IDisposable
    {
        #region Properties
        [Inject] public IJSRuntime JsRuntime { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        //[Inject] public IMessageBroker MessageBroker { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        public List<SportsSortOrder> ClientSportOrderList { get; set; }
        protected bool IsPreLoader { get; set; } = false;
        public string CopyClientName { get; set; }
        public string CopyClientCode { get; set; }
        public string CopyToClientCode { get; set; }
        public string CopyToClientName { get; set; }
        public ElementReference CopyButton { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {

            CopyClientCode = ModalParameters.TryGet<string>("CopyClientCode");
            CopyClientName = ModalParameters.TryGet<string>("CopyClientName");
            CopyToClientCode = ModalParameters.TryGet<string>("CopyToClientCode");
            CopyToClientName = ModalParameters.TryGet<string>("CopyToClientName");
            ClientSportOrderList = ModalParameters.TryGet<List<SportsSortOrder>>("ClientSportOrderList");

            IsPreLoader = false;
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        protected async Task OnCopySportOrder()
        {
            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);

            //int sortOrder = 0;
            //var newSportOrder = new List<SportsSortOrder>();
            //foreach (var sport in ClientSportOrderList)
            //{
            //    if (sport.IsActive)
            //    {
            //        sortOrder++;
            //        var sportOrder = new SportsSortOrder()
            //        {
            //            SportId = sport.SportId,
            //            SportName = sport.SportName,
            //            SortOrder = sortOrder,
            //            IsActive = sport.IsActive,
            //            SegmentsSortOrder = new List<SportsSegmentSortOrder>()
            //        };

            //        int segmentOrderCtr = 0;
            //        var segmentOrders = new List<SportsSegmentSortOrder>();
            //        var segments = sport.SegmentsSortOrder.Where(s => s.IsActive).ToList();
            //        if (segments.Any())
            //        {
            //            foreach (var segment in segments)
            //            {
            //                segmentOrderCtr++;
            //                var segmentOrder = new SportsSegmentSortOrder
            //                {
            //                    SportId = segment.SportId,
            //                    SportName = segment.SportName,
            //                    SegmentId = segment.SegmentId,
            //                    SegmentName = segment.SegmentName,
            //                    SortOrder = segmentOrderCtr,
            //                    IsActive = segment.IsActive,
            //                };

            //                int leagueOrderCtr = 0;
            //                var leagueOrders = new List<SportSegmentLeagueSortOrder>();
            //                var leagues = segment.LeagueSortOrder.Where(s => s.IsActive).ToList();
            //                if (leagues.Any())
            //                {
            //                    foreach (var league in leagues)
            //                    {
            //                        leagueOrderCtr++;
            //                        leagueOrders.Add(new SportSegmentLeagueSortOrder
            //                        {
            //                            SportId = league.SportId,
            //                            SportName = league.SportName,
            //                            SegmentId = league.SegmentId,
            //                            SegmentName = league.SegmentName,
            //                            LeagueId = league.LeagueId,
            //                            LeagueName = league.LeagueName,
            //                            SortOrder = leagueOrderCtr,
            //                            IsActive = league.IsActive,
            //                        });
            //                    }
            //                }
            //                segmentOrder.LeagueSortOrder = leagueOrders;

            //                segmentOrders.Add(segmentOrder);
            //            }
            //            sportOrder.SegmentsSortOrder = segmentOrders;
            //        }

            //        newSportOrder.Add(sportOrder);

            //    }
            //}

            var request = new ClientSportSettingRequest
            {
                ClientCode = CopyToClientCode,
                UserId = UserModel.Id,
                UserName = UserModel.UserName,
                TimeStamp = DateTime.UtcNow,
                SportsSortOrder = ClientSportOrderList,
            };

            var response = (await TradingService.AddOrUpdateClientSportSettings(request)).Result;

            if (response.StatusCode == StatusCode.Success)
            {
                //await JsRuntime.InvokeVoidAsync("ShowNotication", CopyButton, $"{CopyClientName} Sports Settings is successfully copied to {CopyToClientName}");

                await InvokeAsync(StateHasChanged);

                //await InitializeCallback(CopyToClientCode);
            }

            var AlertMessages = response.StatusCode == StatusCode.Success ? $"{CopyClientName} Sports Settings is successfully copied to {CopyToClientName}" : $"Copy Sport Settings for {CopyToClientName} from {CopyToClientName} Failed!";

            //var notificationsDetailsModel = new NotificationsDetailsModel
            //{
            //    Id = Guid.NewGuid(),
            //    Page = "Sports Settings",
            //    ClientCode = CopyToClientCode,
            //    SportsId = 0,
            //    Details = AlertMessages,
            //    PageRedirect = "/administration/client/sportorder",
            //    UserId = Guid.Parse(UserModel.User.Id),
            //    ClaimType = ClaimType.Administration_SportsSortOrder_Access.ToString().Replace("_", "."),
            //    Type = (int)NotificationType.General
            //};
            //MessageBroker.Publish(JsonSerializer.Serialize(notificationsDetailsModel), "OnNewNotification", "r.oe.util.notifications", Guid.NewGuid().ToString());


            IsPreLoader = false;

            ModalService.Cancel();
        }

        public void Dispose()
        {
        }

        #endregion
    }
}