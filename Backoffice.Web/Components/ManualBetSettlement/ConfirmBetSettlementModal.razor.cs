using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Components.ManualBetSettlement
{
    public partial class ConfirmBetSettlementModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        private ManualBetDetailsModel ManualBetDetailsModel { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            ManualBetDetailsModel = ModalParameters.TryGet<ManualBetDetailsModel>("ManualBetDetailsModel");
        }
        public async Task OnModalCancel()
        {
            ModalParameters parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 80rem");
            parameters.Add("ManualBetDetailsModel", ManualBetDetailsModel);

            ModalOptions options = new ModalOptions();

            ModalService.Open<AddReferenceModal>("Add Reference ID", parameters, options);
        }

        public async Task OnModalConfirm()
        {
            OnActivityLogListMessage ActivityLogs = new OnActivityLogListMessage();
            foreach (var bet in ManualBetDetailsModel.Bets)
            {
                OnActivityLogMessage ActivityLog = new OnActivityLogMessage()
                {
                    Page = Page.Administration_Settlement_Bet.ToString().Replace('_','.'),
                    Activity = Activity.ManualSettlement.ToString(),
                    Payload = $"{bet.ClientCode}.{bet.Selections.FirstOrDefault().SportId}.{bet.BetId}",
                    From = "-",
                    To = JsonSerializer.Serialize(bet),
                    UserId = UserModel.Id,
                    UserName = UserModel.UserName,
                    TimeStamp = DateTime.Now
                };

                ActivityLogs.OnActivityLogMessages.Add(ActivityLog);
            }

            var response = (await BackOfficeService.ManualSettlement(new ManualBetSettlementRequest()
            {
                BetData = ManualBetDetailsModel.Bets.Select(bet => new ManualBetRequest()
                {
                    BetId = bet.BetId.ToString(),
                    NewStatus = (BetStatus) bet.BetStatusId,
                    Reason = bet.Reason,
                }).ToList(),
                ActivityLogs = ActivityLogs
            })).Result;

            ModalService.Cancel();
        }

        public void Dispose()
        {

        }
        #endregion
    }
}
