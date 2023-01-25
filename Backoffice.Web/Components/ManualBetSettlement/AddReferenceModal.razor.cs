using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text;
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
        private List<string> BetIds { get; set; }
        private ManualBetSettlementModel ManualBetSettlementModel { get; set; }
        private ManualBetDetailsModel ManualBetDetailsModel { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            BetIds = new List<string>();
            ManualBetSettlementModel = new ManualBetSettlementModel();
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
        }

        public async Task OnReferenceIdOnChangeEvent(ChangeEventArgs e)
        {
            Guid betId;

            bool isGuid = Guid.TryParse(e.Value.ToString(), out betId);

            if (isGuid)
            {
                BetIds.Add(betId.ToString());

                var response = (await BackOfficeService.GetManualBetDetails(new ManualBetDetailsRequest()
                {
                    BetIds = BetIds
                })).Result;

                if (response.Bets.Any())
                {
                    ManualBetDetailsModel = new ManualBetDetailsModel();

                    foreach (var bet in response.Bets)
                    {
                        ManualBetDetails BetDetails = AssignBetDetailsProperties(bet);

                        ManualBetDetailsModel.Bets.Add(BetDetails);
                    }
                }
            }
        }

        public async Task OnBetStatusChangedEvent(ChangeEventArgs e, ManualBetDetails bet)
        {
            bet.BetStatusId = int.Parse(e.Value.ToString());

            switch ((BetStatus)bet.BetStatusId)
            {
                case BetStatus.Won:
                    {
                        bet.BetStatusName = BetStatus.Won.ToString();
                        break;
                    }
                case BetStatus.Lost:
                    {
                        bet.BetStatusName = BetStatus.Lost.ToString();
                        break;
                    }
                case BetStatus.HalfWon:
                    {
                        bet.BetStatusName = BetStatus.HalfWon.ToString();
                        break;
                    }
                case BetStatus.HalfLost:
                    {
                        bet.BetStatusName = BetStatus.HalfLost.ToString();
                        break;
                    }
                case BetStatus.Void:
                    {
                        bet.BetStatusName = BetStatus.Void.ToString();
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
        private ManualBetDetails AssignBetDetailsProperties(ManualBetDetailsResponse.ManualBetDetails bet)
        {
            return new ManualBetDetails()
            {
                BetId = bet.BetId,
                UserId = bet.UserId,
                Stake = bet.Stake,
                TrueStake = bet.TrueStake,
                Odds = bet.Odds,
                OddsTypeId = bet.OddsTypeId,
                OddsTypeName = bet.OddsTypeName,
                Payout = bet.Payout,
                WinLoss = bet.WinLoss,
                BetStatusId = bet.BetStatusId,
                BetStatusName = bet.BetStatusName,
                Reason = bet.Reason,
                CurrencyCode = bet.CurrencyCode,
                LanguageCode = bet.LanguageCode,
                ExchangeRate = bet.ExchangeRate,
                IPAddress = bet.IPAddress,
                Channel = bet.Channel,
                ClientId = bet.ClientId,
                ClientIdRef = bet.ClientIdRef,
                ClientCode = bet.ClientCode,
                ClientName = bet.ClientName,
                LimitId = bet.LimitId,
                Endpoint = bet.Endpoint,
                OperatorToken = bet.OperatorToken,
                CompanyShare = bet.CompanyShare,
                IsResettled = bet.IsResettled,
                IsCashout = bet.IsCashout,
                BetDate = bet.BetDate,
                BetDateOffset = bet.BetDateOffset,
                SettlementDate = bet.SettlementDate,
                SettlementDateOffset = bet.SettlementDateOffset,
                StakeTax = bet.StakeTax,
                WinningsTax = bet.WinningsTax,
                TaxedStake = bet.TaxedStake,
                TaxedWinnings = bet.TaxedWinnings,
                ParlayBonus = bet.ParlayBonus,
                ParlayBonusWinnings = bet.ParlayBonusWinnings,
                StakeReal = bet.StakeReal,
                StakeBonus = bet.StakeBonus,
                StakeRealPayout = bet.StakeRealPayout,
                StakeBonusPayout = bet.StakeBonusPayout,
                IsFreeBet = bet.IsFreeBet,
                IsBoosted = bet.IsBoosted,
                Selections = bet.Selections.Select(betSelection => new BetSelectionDetails()
                {
                    BetId = betSelection.BetId,
                    BetSelectionId = betSelection.BetSelectionId,
                    SportId = betSelection.SportId,
                    SportName = betSelection.SportName,
                    SegmentId = betSelection.SegmentId,
                    SegmentName = betSelection.SegmentName,
                    LeagueId = betSelection.LeagueId,
                    LeagueName = betSelection.LeagueName,
                    FixtureId = betSelection.FixtureId,
                    FixtureName = betSelection.FixtureName,
                    FixtureTypeId = betSelection.FixtureTypeId,
                    FixtureTypeName = betSelection.FixtureTypeName,
                    MarketTemplateId = betSelection.MarketTemplateId,
                    MarketId = betSelection.MarketId,
                    MarketName = betSelection.MarketName,
                    SelectionTemplateId = betSelection.SelectionTemplateId,
                    SelectionId = betSelection.SelectionId,
                    SelectionName = betSelection.SelectionName,
                    FixtureStatusId = betSelection.FixtureStatusId,
                    FixtureStatusName = betSelection.FixtureStatusName,
                    Line = betSelection.Line,
                    Odds = betSelection.Odds,
                    BetSelectionStatusId = betSelection.BetSelectionStatusId,
                    BetSelectionStatusName = betSelection.BetSelectionStatusName,
                    IsSettled = betSelection.IsSettled,
                    IsResettled = betSelection.IsResettled,
                    FixtureSpecifier = betSelection.FixtureSpecifier,
                    SelectionSpecifier = betSelection.SelectionSpecifier,
                    FixtureStartDate = betSelection.FixtureStartDate,
                    BetScore = betSelection.BetScore,
                    SettlementScore = betSelection.SettlementScore,
                    Probability = betSelection.Probability,
                    WonProbability = betSelection.WonProbability,
                    LostProbability = betSelection.LostProbability,
                    VoidProbability = betSelection.VoidProbability,
                    HalfWonProbability = betSelection.HalfWonProbability,
                    HalfLostProbability = betSelection.HalfLostProbability,
                    CorrelationId = betSelection.CorrelationId,
                    DeadHeatFactor = betSelection.DeadHeatFactor,
                    UserId = betSelection.UserId
                }).ToList()
            };
        }
        private string BuildEventDetails(BetSelectionDetails betSelectionDetails, string oddsFormat)
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

        private string BuildBetDate(ManualBetDetails bet)
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
