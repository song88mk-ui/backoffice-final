using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text;
using System;
using Backoffice.Web.Components.ResendSettlement;

namespace Backoffice.Web.Pages.Administration
{
    public partial class ResendSettlement : IDisposable
    {
        #region Properties
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [Inject] private IModalService ModalService { get; set; }
        private List<ResendSettlementBetDetailModel> ResendSettlementModel { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public HashSet<string> BetIds { get; set; }        
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Resend Settlement";
            IconClass = "ico ico-resend-settlement";

            BetIds = new HashSet<string>();

            ResendSettlementModel = new List<ResendSettlementBetDetailModel>();

            if (ResendSettlementModel.Count == 0)
            {
                ResendSettlementModel.Add(new ResendSettlementBetDetailModel());
            }
        }

        public async Task OnAddRow()
        {
            ResendSettlementModel.Add(new ResendSettlementBetDetailModel());
        }

        public async Task OnReferenceIdOnChangeEvent(ChangeEventArgs e, int index)
        {
            Guid betId;

            if (string.IsNullOrEmpty(e.Value.ToString()))
            {
                BetIds.Remove(ResendSettlementModel[index].Bet.BetId.ToString());
                ResendSettlementModel[index] = new ResendSettlementBetDetailModel();                
            }
            else
            {
                bool isGuid = Guid.TryParse(e.Value.ToString(), out betId);

                if (isGuid)
                {
                    BetIds.Add(betId.ToString());

                    var TempResendSettlementModel = new List<ResendSettlementBetDetailModel>();

                    foreach (var betIdItem in BetIds)
                    {
                        var response = (await BackOfficeService.GetResendSettlementBetDetails(new ResendSettlementBetDetailRequest()
                        {
                            BetId = betIdItem.ToString()
                        })).Result;

                        if (response != null)
                        {
                            TempResendSettlementModel.Add(response);
                        }
                    }

                    ResendSettlementModel = TempResendSettlementModel;
                }
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

        public async Task OnResendSettlement()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("ResendSettlementModel", ResendSettlementModel);

            var options = new ModalOptions();
            ModalService.Open<ConfirmationModal>("Resend Settlement", parameters, options);
        }

        public async Task OnResetSettlement()
        {
            var parameters = new ModalParameters();            
            parameters.Add("ModalDialogClass", "modal-lg");

            var options = new ModalOptions();
            ModalService.Open<ResetSettlementModal>("Reset Settlement Entries", parameters, options);
        }

        public async Task ResetSettlementEntries()
        {
            BetIds = new HashSet<string>();
            ResendSettlementModel = new List<ResendSettlementBetDetailModel>() { new ResendSettlementBetDetailModel() };

            await InvokeAsync(StateHasChanged);
        }

        public async Task OnBetDelete(ResendSettlementBetDetailModel bet)
        {
            ResendSettlementModel.Remove(bet);
            BetIds.Remove(bet.Bet.BetId.ToString());

            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
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
    }
    #endregion
}
