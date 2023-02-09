using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Administration.ResendSettlement;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text;

namespace Backoffice.Web.Pages.Administration
{
    public partial class ResendSettlement : IDisposable
    {
        #region Properties

        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [Inject] private IModalService ModalService { get; set; }

        public List<AlertModel> Alerts { get; set; }
        private List<ResendSettlementBetDetailModel> ResendSettlementModel { get; set; }        

        public string PageTitle { get; set; }
        public string IconClass { get; set; }        
        public bool IsAddDisabled { get; set; }
        public bool IsActionDisabled { get; set; }
        public Guid Guid { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Resend Settlement";
            IconClass = "ico ico-resend-settlement";
            IsAddDisabled = true;
            IsActionDisabled = false;
            Guid = new Guid();
            
            ResendSettlementModel = new List<ResendSettlementBetDetailModel>();
            Alerts = new List<AlertModel>();

            if (ResendSettlementModel.Count == 0)
            {
                ResendSettlementModel.Add(new ResendSettlementBetDetailModel());
            }
        }

        public async Task OnAddRowClickEvent()
        {
            ResendSettlementModel.Add(new ResendSettlementBetDetailModel());
            IsAddDisabled = true;
        }

        public async Task OnReferenceIdOnChangeEvent(ChangeEventArgs e, ResendSettlementBetDetailModel betDetailModel, int index)
        {
            Guid betId;           

            if (string.IsNullOrEmpty(e.Value.ToString()))
            {                
                ResendSettlementModel[index] = new ResendSettlementBetDetailModel();                
            }
            else
            {
                IsActionDisabled = true;

                IsAddDisabled = true;

                bool isGuid = Guid.TryParse(e.Value.ToString(), out betId);

                if (isGuid)
                {
                    //check for duplicates
                    if (ResendSettlementModel != null && ResendSettlementModel.Any() && ResendSettlementModel.Any(x => x.Bet.BetId == betId))
                    {
                        Alerts = new List<AlertModel>();

                        Alerts.Add(new AlertModel()
                        {
                            Status = "Error",
                            Message = "Duplicate BetId's are not allowed",
                            Opacity = 1.0m
                        });

                        Guid = Guid.NewGuid();
                    }
                    else
                    {
                        var response = (await BackOfficeService.GetResendSettlementBetDetails(new ResendSettlementBetDetailRequest()
                        {
                            BetId = betId.ToString()
                        })).Result;

                        if (response != null)
                        {
                            ResendSettlementModel[index] = response;
                        }
                    }                    
                    
                    IsAddDisabled = false;
                }

                IsActionDisabled = false;
            }
        }

        public async Task OnDeleteReference(ResendSettlementBetDetailModel bet)
        {
            var parameters = new ModalParameters();
            parameters.Add("OnBetDeleteCallback", EventCallback.Factory.Create<ResendSettlementBetDetailModel>(this, OnBetDelete));
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("ResendSettlementBetDetailModel", bet);

            var options = new ModalOptions();
            ModalService.Open<DeleteReferenceIdModal>("Delete Reference ID", parameters, options);
        }

        public async Task OnResendSettlementClickEvent()
        {
            if (ResendSettlementModel != null && ResendSettlementModel.Any())
            {                
                if (ResendSettlementModel.Any(x => x.Bet.BetStatusId == (int)BetStatus.Rejected))
                {
                    Alerts.Add(new AlertModel()
                    {
                        Status = StatusCode.Error.ToString(),
                        Message = "Rejected bet are not allowed to be resent to Operator",
                        Opacity = 1.0m
                    });

                    Guid = Guid.NewGuid();
                }
                else
                {
                    var parameters = new ModalParameters();
                    parameters.Add("ModalDialogClass", "modal-lg");
                    parameters.Add("OnResendSettlementCallback", EventCallback.Factory.Create<BaseResponse<ResendSettlementResponse>>(this, OnResendSettlementCallback));
                    parameters.Add("ResendSettlementModel", ResendSettlementModel);

                    var options = new ModalOptions();
                    ModalService.Open<ConfirmationModal>("Resend Settlement", parameters, options);
                }                
            }
        }

        public async Task OnResetSettlementClickEvent()
        {
            var parameters = new ModalParameters();            
            parameters.Add("ModalDialogClass", "modal-lg");

            var options = new ModalOptions();
            ModalService.Open<ResetSettlementModal>("Reset Settlement Entries", parameters, options);
        }

        public async Task ResetSettlementEntries()
        {            
            ResendSettlementModel = new List<ResendSettlementBetDetailModel>() { new ResendSettlementBetDetailModel() };            
        }

        public async Task OnBetDelete(ResendSettlementBetDetailModel bet)
        {
            ResendSettlementModel.Remove(bet);             
        }

        public async Task OnResendSettlementCallback(BaseResponse<ResendSettlementResponse> response)
        {
            Alerts = new List<AlertModel>();

            if (!response.Result.Data.Any())
            {
                Alerts.Add(new AlertModel()
                {
                    Status = StatusCode.Error.ToString(),
                    Message = StatusCode.Resend_Attempt_Not_Successful_Please_Try_Again.ToString().Replace('_', ' '),
                    Opacity = 1.0m
                });

                Guid = Guid.NewGuid();
            }
            else if (response.StatusCode == StatusCode.Error)
            {
                Alerts.Add(new AlertModel()
                {
                    Status = response.StatusCode.ToString(),
                    Message = response.Result.Data.FirstOrDefault().Message,
                    Opacity = 1.0m
                });

                Guid = Guid.NewGuid();
            }
            else
            {
                Alerts.Add(new AlertModel()
                {
                    Status = response.StatusCode.ToString(),
                    Message = "Resend to operator of bet settlement, cashout, and/or cancellation successful",
                    Opacity = 1.0m
                });

                Guid = Guid.NewGuid();
            }

            await InvokeAsync(StateHasChanged);
            await Task.Delay(3500);
        }

        public void Dispose()
        {
            BackOfficeService.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        private string BuildEventDetails(ResendSettlementBetDetailModel bet)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("<div class=\"information-container\">");

            if (bet.Bet.BetSelections == null || bet.Bet.BetSelections.Count == 0)
            {
                builder.Append("<span>-</span>");
            }
            else
            {
                var betSelection = bet.Bet.BetSelections.FirstOrDefault();
                builder.Append($"<span class=\"segment-league d-block\">{betSelection.LeagueName}</span>");
                builder.Append($"<span class=\"fixture d-block\">{betSelection.FixtureName}</span>");
                builder.Append($"<span class=\"segment-league d-block\">{betSelection.FixtureStartDate.ToString("MM/dd/yyyy hh:mm:ss tt").ToUpper()}</span>");
                builder.Append($"<span class=\"selection-details d-block\">{betSelection.SelectionName}</span>");
                builder.Append($"<span class=\"score d-block\">(Bet Result: {betSelection.BetScore}, Final Score: {betSelection.SettlementScore})</span>");
                builder.Append($"<span class=\"status\">{betSelection.FixtureStatusName}</span>");
            }

            builder.Append("<div/>");

            return builder.ToString();
        }

        private string BuildBetDate(ResendSettlementBetDetailModel bet)
        {
            StringBuilder builder = new StringBuilder();

            if (bet.Bet.BetDate == DateTime.MinValue)
            {
                builder.Append("<span>-</span>");
            }
            else
            {
                builder.Append($"<span class=\"bet-date d-block\">{bet.Bet.BetDate.ToString("MM/dd/yyyy")}</span>");
                builder.Append($"<span class=\"bet-time d-block\">{bet.Bet.BetDate.ToString("hh:mm:ss tt").ToUpper()}</span>");
            }

            return builder.ToString();
        }

        #endregion
    }
}
