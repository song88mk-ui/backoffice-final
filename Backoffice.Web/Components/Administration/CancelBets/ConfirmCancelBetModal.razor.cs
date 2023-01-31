using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Text.Json;

namespace Backoffice.Web.Components.Administration.CancelBets
{
    public partial class ConfirmCancelBetModal : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] private IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        public CancelBetModel CancelBetModel { get; set; }
        public List<AlertModel> Alerts { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Alerts = new List<AlertModel>();
            CancelBetModel = ModalParameters.TryGet<CancelBetModel>("CancelBetModel");
        }

        public async Task OnCloseModalClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnConfirmCancelClickEvent()
        {
            try
            {
                Alerts.Clear();

                var betIds = CancelBetModel.BetReferenceIds.Where(b => !string.IsNullOrEmpty(b.BetReferenceId)).Select(s => s.BetReferenceId).ToList();

                var betDetails = (await BackOfficeService.GetManualBetDetails(new ManualBetDetailsRequest
                {
                    BetIds = betIds,
                })).Result;


                if(betDetails.Bets.Any(b => b.BetStatusId != (int)BetStatus.Running))
                {
                    Alerts.Add(new AlertModel
                    {
                        AlertStatus = "Error",
                        AltertMessage = $"Only Running Status should be settled as Cancelled",
                        Opacity = 1.0m
                    });
                    return;
                }

                var request = new BetModifyRequest
                {
                    BetIds = betIds,
                    NewStatus = BetStatus.Cancelled,
                    Reason = CancelBetModel.Reason,
                    ModifiedByUserId = 0,
                };

                var response = (await BackOfficeService.CancelBet(request));

                if (response.StatusCode == StatusCode.Success)
                {
                    
                    //activity log
                    var message = new OnActivityLogListMessage();
                    foreach (var data in response.Result.Data)
                    {
                        var betData = betDetails.Bets.FirstOrDefault(b => b.BetId == Guid.Parse(data.BetId));

                        if (betData != null)
                        {
                            betData.Reason = request.Reason;
                            betData.SettlementDate = DateTime.UtcNow;

                            if(betData.Selections.Any())
                            {
                                foreach(var selection in betData.Selections)
                                {
                                    message.OnActivityLogMessages.Add(new OnActivityLogMessage
                                    {
                                        Page = Page.Admin_CancelBets.ToString().Replace("_", "."),
                                        Payload = $"{UserModel.UserName}.{data.BetId}.{selection.BetSelectionId}.{betData.UserId}.{betData.ClientName}",
                                        Activity = Activity.CancelBet.ToString(),
                                        From = JsonSerializer.Serialize(betData),
                                        To = JsonSerializer.Serialize(selection),
                                        UserId = UserModel.Id,
                                        UserName = UserModel.UserName,
                                    });
                                }
                            }
                        }
                    }

                    await BackOfficeService.AddActivityLogList(message);

                    Alerts.Add(new AlertModel
                    {
                        AlertStatus = "Success",
                        AltertMessage = "Bet has been successfully cancelled",
                        Opacity = 1.0m
                    });

                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(3500);

                    ModalService.Cancel();

                    await Modal.OnSaveClickCallback.InvokeAsync();
                }
                else
                {
                    if(response.Result != null && response.Result.Data != null)
                    {
                        foreach(var data in response.Result.Data)
                        {
                            if(data.IsSuccess == false)
                            {
                                Alerts.Add(new AlertModel
                                {
                                    AlertStatus = "Error",
                                    AltertMessage = $"{data.Message}",
                                    Opacity = 1.0m
                                });
                            }

                        }
                    }
                    else
                    {
                        Alerts.Add(new AlertModel
                        {
                            AlertStatus = "Error",
                            AltertMessage = $"{response.Message}",
                            Opacity = 1.0m
                        });
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public void Dispose()
        {

        }
        #endregion
    }
}
