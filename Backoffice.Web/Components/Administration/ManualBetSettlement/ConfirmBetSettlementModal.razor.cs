using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Components.Administration.ManualBetSettlement
{
    public partial class ConfirmBetSettlementModal : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackofficeService BackofficeService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] private CustomModal Modal { get; set; }

        private ManualBetDetailsModel ManualBetDetailsModel { get; set; }
        public bool IsPreload { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            IsPreload = false;
            ManualBetDetailsModel = ModalParameters.TryGet<ManualBetDetailsModel>("ManualBetDetailsModel");
        }

        public async Task OnModalCancel()
        {
            if (IsPreload)
                return;

            ModalParameters parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 80rem");
            parameters.Add("ManualBetDetailsModel", ManualBetDetailsModel);

            ModalOptions options = new ModalOptions();

            ModalService.Open<AddReferenceModal>("Add Reference ID", parameters, options);
        }

        public async Task OnModalConfirm()
        {
            if (IsPreload)
                return;

            IsPreload = true;

            OnActivityLogListMessage ActivityLogs = new OnActivityLogListMessage();
            foreach (var bet in ManualBetDetailsModel.Bets)
            {
                OnActivityLogMessage ActivityLog = new OnActivityLogMessage()
                {
                    Page = AdminPermission.Admin_Settlements_ManualBetSettlement.ToString().Replace('_', '.'),
                    Activity = Activity.ManualSettlement.ToString(),
                    Payload = JsonSerializer.Serialize(bet),
                    From = "-",
                    To = bet.BetStatusName,
                    UserId = _userState.Model.Id,
                    UserName = _userState.Model.Name,
                    TimeStamp = DateTime.UtcNow
                };

                ActivityLogs.OnActivityLogMessages.Add(ActivityLog);
            }

            var response = (await BackofficeService.ManualSettlement(new ManualBetSettlementRequest()
            {
                BetData = ManualBetDetailsModel.Bets.Select(bet => new ManualBetRequest()
                {
                    BetId = bet.BetId.ToString(),
                    NewStatus = (BetStatus)(int.Parse(bet.NewBetStatusId)),
                    Reason = bet.NewReason,
                }).ToList(),
                ActivityLogs = ActivityLogs
            }));

            await Modal.OnSaveClickCallback.InvokeAsync(response);

            ModalService.Cancel();

            IsPreload = false;
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
