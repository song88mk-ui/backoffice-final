using Backoffice.Web.Components.Administration.CancelBets;
using Backoffice.Web.Components.Administration.ManageUserGroups;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.Administration
{
    public partial class CancelBets : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        public CancelBetModel CancelBetModel { get; set; }
        public List<AlertModel> Alerts { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Alerts = new List<AlertModel>();
            CancelBetModel = new CancelBetModel();
            CancelBetModel.BetReferenceIds.Add(new CancelBetModel.BetReferenceModel
            {
                Num = 1,
                BetReferenceId =string.Empty
            });

            PageTitle = "Cancel Bets";
            IconClass = "ico ico-cancel-bets";
        }

        public async Task OnAddBetReferenceIdCallbackEvent()
        {
            if(CancelBetModel.BetReferenceIds.Count() <= 9)
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
            if (!Validate())
                return;

            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("CancelBetModel", CancelBetModel);

            var options = new ModalOptions();
            ModalService.Open<ConfirmCancelBetModal>("Cancel Bets", parameters, options);
        }
        public async Task OnResetCallbackEvent()
        {
            CancelBetModel = new CancelBetModel();
            CancelBetModel.BetReferenceIds.Clear();
            CancelBetModel.BetReferenceIds.Add(new CancelBetModel.BetReferenceModel
            {
                Num = 1,
                BetReferenceId = string.Empty
            });

        }
        public void Dispose()
        {
        }

        #endregion

        #region Utilities
        private bool Validate()
        {
            Alerts.Clear();

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
                            Status = "Error",
                            Message = $"Invalid Bet Reference Id {bet.BetReferenceId.Trim()}",
                            Opacity = 1.0m
                        });
                    }

                    var existingBet = CancelBetModel.BetReferenceIds.FirstOrDefault(b => b.Num != bet.Num && b.BetReferenceId.Trim() == bet.BetReferenceId.Trim());
                    if (existingBet != null)
                    {
                        Alerts.Add(new AlertModel
                        {
                            Status = "Error",
                            Message = $"Duplicate Bet Reference Id found. {bet.BetReferenceId.Trim()}",
                            Opacity = 1.0m
                        });
                    }

                    count++;
                }
            }

            if (count == 0)
            {
                Alerts.Add(new AlertModel
                {
                    Status = "Error",
                    Message = $"At least 1 Bet Reference Id is required.",
                    Opacity = 1.0m
                });
            }

            if (string.IsNullOrEmpty(CancelBetModel.Reason))
            {
                Alerts.Add(new AlertModel
                {
                    Status = "Error",
                    Message = $"Please specify reason for canceling bet/s.",
                    Opacity = 1.0m
                });
            }

            if (Alerts.Any())
                return false;

            return true;

        }
        #endregion
    }
}
