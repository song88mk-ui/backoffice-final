using Backoffice.Shared.Enums;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Web.Components.Administration.CancelBets;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Serilog;
using System.Text.Json;

namespace Backoffice.Web.Pages.Administration
{
    public partial class CancelBets : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }

        private CancelBetReferenceModel BetRefenceModel;
        private List<CancelBetAlertModel> Alerts;

        private PageHeaderModel _pageHeaderModel;
        public bool IsPreload { get; set; }
        public bool IsReasonRequired { get; set; }

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Alerts = new();
            BetRefenceModel = new()
            {
                BetReferenceIds = new() { string.Empty }
            };

            await AuthenticateAccess.InvokeAsync();
            _pageHeaderModel = new()
            {
                Title = "Cancel Bets",
                IconClass = "ico ico-cancel-bets"
            };
        }

        private void OnAddBetReferenceIdCallbackEvent()
        {
            if (BetRefenceModel.BetReferenceIds != null && BetRefenceModel.BetReferenceIds.Any() && BetRefenceModel.BetReferenceIds.Count() <= 9)
            {
                BetRefenceModel.BetReferenceIds.Add(string.Empty);
            }
        }
        private void OnCancelBetCallbackEvent()
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
        private void OnResetCallbackEvent()
        {
            IsPreload = false;
            IsReasonRequired = false;

            Alerts.Clear();
            BetRefenceModel.BetReferenceIds.Clear();
            BetRefenceModel.BetReferenceIds.Add(string.Empty);
        }

        public async Task OnConfirmCancelBetEvent()
        {
            try
            {
                IsPreload = true;
                Alerts.Clear();

                var betIds = BetRefenceModel.BetReferenceIds.Where(x => !string.IsNullOrEmpty(x)).Select(x => x).ToList();

                var manualBetDetailsResponse = await BackOfficeService.GetManualBetDetails(new() { BetIds = betIds });
                var betDetails = manualBetDetailsResponse.Result;

                var notRunningBets = betDetails.Bets.Where(b => b.BetStatusId != (int)BetStatus.Running).ToList();

                foreach (var bet in BetRefenceModel.BetReferenceIds)
                {
                    if (string.IsNullOrEmpty(bet))
                        continue;

                    if (notRunningBets.Any(b => b.BetId == Guid.Parse(bet)))
                    {
                        Alerts.Add(new()
                        {
                            Status = AlertStatus.Error,
                            Message = $"Your request to cancel this Bet Reference ID did not proceed. Only 'Running' bets can be settled as 'Cancelled'.",
                            Opacity = 1.0m,
                            Control = $"{BetRefenceModel.BetReferenceIds.IndexOf(bet)}"
                        });
                        IsPreload = false;
                    }
                }

                if (Alerts.Any())
                    return;

                var reason = BetRefenceModel.Reason;
                var response = (await BackOfficeService.CancelBet(new()
                {
                    BetIds = betIds,
                    NewStatus = BetStatus.Cancelled,
                    Reason = BetRefenceModel.Reason,
                    ModifiedByUserId = 0,
                }));

                if (response.StatusCode == StatusCode.Success)
                {
                    //activity log
                    OnActivityLogListMessage message = new();
                    foreach (var data in response.Result.Data)
                    {
                        var betData = betDetails.Bets.FirstOrDefault(b => b.BetId == Guid.Parse(data.BetId));

                        if (betData != null)
                        {
                            betData.Reason = reason;
                            betData.SettlementDate = DateTime.UtcNow;

                            if (betData.Selections.Any())
                            {
                                foreach (var selection in betData.Selections)
                                {
                                    message.OnActivityLogMessages.Add(new()
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

                    Alerts.Add(new()
                    {
                        Status = AlertStatus.Success,
                        Message = "Bet Reference IDs has been successfully cancelled"
                    });

                    BetRefenceModel = new()
                    {
                        BetReferenceIds = new() { "" }
                    };

                }
                else
                {
                    if (response.Result != null && response.Result.Data != null)
                    {
                        foreach (var data in response.Result.Data.Where(x => !x.IsSuccess))
                        {
                            Alerts.Add(new()
                            {
                                Status = AlertStatus.Error,
                                Message = $"{data.Message}"
                            });

                        }
                    }
                    else
                    {
                        Alerts.Add(new()
                        {
                            Status = AlertStatus.Error,
                            Message = $"{response.Message}"
                        });
                    }
                }
                IsPreload = false;

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }
        }
        public void Dispose()
        {
            BetRefenceModel = new();
            Alerts = new();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities
        private bool Validate()
        {
            Alerts.Clear();
            IsReasonRequired = false;
            var count = 0;
            foreach (var bet in BetRefenceModel.BetReferenceIds)
            {
                if (!string.IsNullOrEmpty(bet.Trim()))
                {
                    var isValid = Guid.TryParse(bet.Trim(), out var betId);
                    if (!isValid)
                    {
                        Alerts.Add(new()
                        {
                            Status = AlertStatus.Error,
                            Message = $"Invalid Bet Reference Id {bet.Trim()}",
                            Opacity = 1.0m,
                            Control = $"{count + 1}"
                        });
                    }

                    var existingBet = BetRefenceModel.BetReferenceIds.FirstOrDefault(b => b.Trim() == bet.Trim());
                    if (existingBet != null)
                    {
                        if (!Alerts.Any(a => a.Message.Contains($"Duplicate Bet Reference Id found. {bet.Trim()}")))
                            Alerts.Add(new()
                            {
                                Status = AlertStatus.Error,
                                Message = $"Duplicate Bet Reference Id found. {bet.Trim()}",
                                Opacity = 1.0m,
                                Control = $"{count + 1}"
                            });
                    }

                    count++;
                }
            }

            if (count == 0 || string.IsNullOrEmpty(BetRefenceModel.Reason))
            {
                var message = "";
                if (count == 0 && string.IsNullOrEmpty(BetRefenceModel.Reason))
                    message = "Please enter 'Bet Reference ID' and 'Reason for Cancelling Bets' to proceed";
                else if (count == 0 && !string.IsNullOrEmpty(BetRefenceModel.Reason))
                    message = "Please enter 'Bet Reference ID' to proceed";
                else if (count > 0 && string.IsNullOrEmpty(BetRefenceModel.Reason))
                    message = "Please enter 'Reason for Cancelling Bets' to proceed";

                if (!string.IsNullOrEmpty(message))
                {
                    Alerts.Add(new()
                    {
                        Status = AlertStatus.Error,
                        Message = message,
                        Control = count == 0 ? $"1" : ""
                    });
                }
                if (string.IsNullOrEmpty(BetRefenceModel.Reason))
                    IsReasonRequired = true;
            }

            if (Alerts.Any())
                return false;

            return true;

        }
        #endregion
    }
}
