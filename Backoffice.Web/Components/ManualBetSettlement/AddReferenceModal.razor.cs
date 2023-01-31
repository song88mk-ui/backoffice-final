using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Text;
using System.Text.Json;
using static Backoffice.Shared.Models.ManualBetDetailsModel;

namespace Backoffice.Web.Components.ManualBetSettlement
{
    public partial class AddReferenceModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackOfficeService BackOfficeService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        public List<AlertModel> Alerts { get; set; }

        private ManualBetSettlementModel ManualBetSettlementModel { get; set; }
        private ManualBetDetailsModel ManualBetDetailsModel { get; set; }

        public bool IsAddDisabled { get; set; }
        public bool IsActionDisabled { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            ManualBetSettlementModel = new ManualBetSettlementModel();
            Alerts = new List<AlertModel>();
            IsAddDisabled = true;
            IsActionDisabled = false;

            ManualBetDetailsModel = ModalParameters.TryGet<ManualBetDetailsModel>("ManualBetDetailsModel");

            if (ManualBetDetailsModel == null)
            {
                ManualBetDetailsModel = new ManualBetDetailsModel();
                ManualBetDetailsModel.Bets.Add(new ManualBetDetails());
            }
        }

        public async Task OnAddReferenceIdClickedEvent()
        {
            ManualBetDetailsModel.Bets.Add(new ManualBetDetails());
            IsAddDisabled = true;
        }

        public async Task OnReferenceIdOnChangeEvent(ChangeEventArgs args, ManualBetDetails bet, int index)
        {
            Guid betId;

            string betIdString = args.Value.ToString();

            bool isGuid = Guid.TryParse(betIdString, out betId);

            if (isGuid)
            {
                IsActionDisabled = true;

                await InvokeAsync(StateHasChanged);

                if (ManualBetDetailsModel != null && ManualBetDetailsModel.Bets.Any() && ManualBetDetailsModel.Bets.Any(bet => bet.BetId == betId))
                {
                    Alerts = new List<AlertModel>();

                    Alerts.Add(new AlertModel()
                    {
                        AlertStatus = "Error",
                        AltertMessage = "Duplicate BetId's are not allowerd",
                        Opacity = 1.0m
                    });
                }
                else
                {
                    var response = (await BackOfficeService.GetManualBetDetails(new ManualBetDetailsRequest()
                    {
                        BetIds = new List<string> { betIdString }
                    })).Result;

                    if (response.Bets.Any())
                    {
                        bet = JsonSerializer.Deserialize<ManualBetDetails>(JsonSerializer.Serialize(response.Bets.FirstOrDefault()));

                        ManualBetDetailsModel.Bets[index] = bet;

                        IsAddDisabled = false;
                    }
                }                
            }

            IsActionDisabled = false;
        }

        public async Task OnBetStatusChangedEvent(ChangeEventArgs e, ManualBetDetails bet)
        {
            bet.NewBetStatusId = int.Parse(e.Value.ToString());

            switch ((BetStatus)bet.NewBetStatusId)
            {
                case BetStatus.Won:
                    {
                        bet.NewBetStatusName = BetStatus.Won.ToString();
                        break;
                    }
                case BetStatus.Lost:
                    {
                        bet.NewBetStatusName = BetStatus.Lost.ToString();
                        break;
                    }
                case BetStatus.HalfWon:
                    {
                        bet.NewBetStatusName = BetStatus.HalfWon.ToString();
                        break;
                    }
                case BetStatus.HalfLost:
                    {
                        bet.NewBetStatusName = BetStatus.HalfLost.ToString();
                        break;
                    }
                case BetStatus.Void:
                    {
                        bet.NewBetStatusName = BetStatus.Void.ToString();
                        break;
                    }
                default: break;
            }
        }

        public async Task OnReasonChangedEvent(ChangeEventArgs e, ManualBetDetails bet)
        {
            bet.Reason = e.Value.ToString();
        }

        public async Task OnDeleteClickEvent(ManualBetDetails bet)
        {
            ManualBetDetailsModel.Bets.Remove(bet);
        }

        public async Task OnModalResetClickEvent()
        {
            ModalParameters parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("ManualBetDetailsModel", ManualBetDetailsModel);

            ModalOptions options = new ModalOptions();

            ModalService.Open<ResetSettlementModal>("Reset Settlement Entries", parameters, options);
        }

        public async Task OnModalSettleClickEvent()
        {
            ModalParameters parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("ManualBetDetailsModel", ManualBetDetailsModel);

            ModalOptions options = new ModalOptions();

            ModalService.Open<ConfirmBetSettlementModal>("Manual Bet Settlement", parameters, options);
        }
        public void Dispose()
        {

        }
        #endregion

        #region Utilities        
        public string BuildEventDetails(BetSelectionDetails betSelectionDetails, string oddsFormat)
        {
            StringBuilder builder = new StringBuilder();

            if (betSelectionDetails == null)
            {
                builder.Append("<span>-</span>");
            }
            else
            {
                builder.Append($"<span>{betSelectionDetails.LeagueName}</span>");
                builder.Append($"<span>{betSelectionDetails.FixtureName}</span>");
                builder.Append($"<span>{betSelectionDetails.FixtureStartDate.ToString("MM/dd/yyyy hh:mm:ss tt").ToUpper()}</span>");
                builder.Append($"<span>{betSelectionDetails.SelectionName}</span>");
                builder.Append($"<span>{betSelectionDetails.MarketName} @ {betSelectionDetails.Odds} ({oddsFormat})</span>");
                builder.Append($"<span>(Bet Result: {betSelectionDetails.BetScore}, Final Score: {betSelectionDetails.SettlementScore})</span>");
                builder.Append($"<span>{betSelectionDetails.BetSelectionStatusName}</span>");
            }

            return builder.ToString();
        }

        public string BuildBetDate(ManualBetDetails bet)
        {
            StringBuilder builder = new StringBuilder();

            if (bet.BetDate == DateTime.MinValue)
            {
                builder.Append("<span>-</span>");
            }
            else
            {
                builder.Append($"<span>{bet.BetDate.ToString("MM/dd/yyyy")}</span>");
                builder.Append($"<span>{bet.BetDate.ToString("HH:mm:ss tt").ToUpper()}</span>");
            }

            return builder.ToString();
        }
        #endregion
    }
}
