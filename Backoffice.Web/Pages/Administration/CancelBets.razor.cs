using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Web.Components.Administration.CancelBets;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Pages.Administration
{
    public partial class CancelBets : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        public CancelBetModel CancelBetModel { get; set; }
        public List<AlertModel> Alerts { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public bool IsPreload { get; set; }
        public bool IsReasonRequired { get; set; }

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Alerts = new List<AlertModel>();
            CancelBetModel = new CancelBetModel();
            CancelBetModel.BetReferenceIds.Add(new CancelBetModel.BetReferenceModel
            {
                Num = 1,
                BetReferenceId = string.Empty
            });

            PageTitle = "Cancel Bets";
            IconClass = "ico ico-cancel-bets";
        }

        public async Task OnAddBetReferenceIdCallbackEvent()
        {
            if (CancelBetModel.BetReferenceIds.Count() <= 9)
            {
                CancelBetModel.BetReferenceIds.Add(new CancelBetModel.BetReferenceModel
                {
                    Num = CancelBetModel.BetReferenceIds.Count + 1,
                    BetReferenceId = string.Empty
                });
            }
        }
        public async Task OnCancelBetCallbackEvent()
        {
            IsPreload = true;

            if (!Validate())
            {
                IsPreload = false;
                return;
            }

            IsPreload = false;

            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");

            var options = new ModalOptions();
            ModalService.Open<ConfirmCancelBetModal>("Cancel Bets", parameters, options);
        }
        public async Task OnResetCallbackEvent()
        {
            IsPreload = false;
            IsReasonRequired = false;

            Alerts.Clear();
            CancelBetModel = new CancelBetModel();
            CancelBetModel.BetReferenceIds.Clear();
            CancelBetModel.BetReferenceIds.Add(new CancelBetModel.BetReferenceModel
            {
                Num = 1,
                BetReferenceId = string.Empty
            });

        }

        public async Task OnConfirmCancelBetEvent()
        {
            try
            {
                IsPreload = true;
                Alerts.Clear();

                var betIds = CancelBetModel.BetReferenceIds.Where(b => !string.IsNullOrEmpty(b.BetReferenceId)).Select(s => s.BetReferenceId).ToList();

                var betDetails = (await BackOfficeService.GetManualBetDetails(new ManualBetDetailsRequest
                {
                    BetIds = betIds,
                })).Result;

                var notRunningBets = betDetails.Bets.Where(b => b.BetStatusId != (int)BetStatus.Running).ToList();

                var ctr = 0;
                foreach(var bet in CancelBetModel.BetReferenceIds)
                {
                    ctr++;
                    if (string.IsNullOrEmpty(bet.BetReferenceId))
                        continue;

                    if(notRunningBets.Any(b => b.BetId == Guid.Parse(bet.BetReferenceId)))
                    {
                        Alerts.Add(new AlertModel
                        {
                            Status = AlertStatus.Error.ToString(),
                            Message = $"Your request to cancel this Bet Reference ID did not proceed. Only 'Running' bets can be settled as 'Cancelled'.",
                            Opacity = 1.0m,
                            Control = $"{ctr}"
                        });
                        IsPreload = false;
                    }
                }

                if (Alerts.Any())
                    return;

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

                            if (betData.Selections.Any())
                            {
                                foreach (var selection in betData.Selections)
                                {
                                    message.OnActivityLogMessages.Add(new OnActivityLogMessage
                                    {
                                        Page = AdminPermission.Admin_CancelBets.ToString().Replace("_", "."),
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
                        Status = AlertStatus.Success.ToString(),
                        Message = "Bet Refrence IDs has been successfully cancelled",
                        Opacity = 1.0m
                    });

                    CancelBetModel = new CancelBetModel()
                    {
                        BetReferenceIds = new List<CancelBetModel.BetReferenceModel>()
                        {
                            new CancelBetModel.BetReferenceModel()
                            {
                                Num = 1,
                                BetReferenceId = ""
                            }
                        }
                    };

                }
                else
                {
                    if (response.Result != null && response.Result.Data != null)
                    {
                        foreach (var data in response.Result.Data)
                        {
                            if (data.IsSuccess == false)
                            {
                                Alerts.Add(new AlertModel
                                {
                                    Status = AlertStatus.Error.ToString(),
                                    Message = $"{data.Message}",
                                    Opacity = 1.0m
                                });
                            }

                        }
                    }
                    else
                    {
                        Alerts.Add(new AlertModel
                        {
                            Status = AlertStatus.Error.ToString(),
                            Message = $"{response.Message}",
                            Opacity = 1.0m
                        });
                    }
                }
                IsPreload = false;

            }
            catch (Exception)
            {

            }
        }

        public async Task OnClearBetReferenceClickEvent(CancelBetModel.BetReferenceModel model)
        {
            model.BetReferenceId = string.Empty;
        }

        public async Task OnDeleteBetReferenceClickEvent(CancelBetModel.BetReferenceModel model)
        {
            if(CancelBetModel.BetReferenceIds.Count > 1)
                CancelBetModel.BetReferenceIds.Remove(model);
        }

        public void Dispose()
        {
            CancelBetModel = null;
            Alerts = null;

            BackOfficeService.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities
        private bool Validate()
        {
            Alerts.Clear();
            IsReasonRequired = false;
            var count = 0;
            foreach (var bet in CancelBetModel.BetReferenceIds)
            {
                if (!string.IsNullOrEmpty(bet.BetReferenceId.Trim()))
                {
                    var isValid = Guid.TryParse(bet.BetReferenceId.Trim(), out var betId);
                    if (!isValid)
                    {
                        Alerts.Add(new AlertModel
                        {
                            Status = AlertStatus.Error.ToString(),
                            Message = $"Invalid Bet Reference Id {bet.BetReferenceId.Trim()}",
                            Opacity = 1.0m,
                            Control = $"{count+1}"
                        });
                    }

                    var existingBet = CancelBetModel.BetReferenceIds.FirstOrDefault(b => b.Num != bet.Num && b.BetReferenceId.Trim() == bet.BetReferenceId.Trim());
                    if (existingBet != null)
                    {
                        if (!Alerts.Any(a => a.Message.Contains($"Duplicate Bet Reference Id found. {bet.BetReferenceId.Trim()}")))
                            Alerts.Add(new AlertModel
                            {
                                Status = AlertStatus.Error.ToString(),
                                Message = $"Duplicate Bet Reference Id found. {bet.BetReferenceId.Trim()}",
                                Opacity = 1.0m,
                                Control = $"{count + 1}"
                            });
                    }

                    count++;
                }
            }

            if (count == 0 || string.IsNullOrEmpty(CancelBetModel.Reason))
            {
                var message = "";
                if (count == 0 && string.IsNullOrEmpty(CancelBetModel.Reason))
                    message = "Please enter 'Bet Reference ID' and 'Reason for Cancelling Bets' to proceed";
                else if (count == 0 && !string.IsNullOrEmpty(CancelBetModel.Reason))
                    message = "Please enter 'Bet Reference ID' to proceed";
                else if (count > 0 && string.IsNullOrEmpty(CancelBetModel.Reason))
                    message = "Please enter 'Reason for Cancelling Bets' to proceed";

                if (!string.IsNullOrEmpty(message))
                {
                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error.ToString(),
                        Message = message,
                        Opacity = 1.0m,
                        Control = count == 0? $"1" : ""
                    });
                }
                if (string.IsNullOrEmpty(CancelBetModel.Reason))
                    IsReasonRequired = true;
            }

            if (Alerts.Any())
                return false;

            return true;

        }
        #endregion
    }
}
