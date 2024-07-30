using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class ManualBetDetailsModel
    {
        #region Properties
        public List<ManualBetDetails> Bets { get; set; }
        #endregion

        #region Ctor
        public ManualBetDetailsModel()
        {
            Bets = new List<ManualBetDetails>();
        }
        #endregion

        #region Nested Class
        public class ManualBetDetails
        {
            public ManualBetDetails()
            {
                Selections = new List<BetSelectionDetails>();
            }
            public string? BetId { get; set; }
            public string UserId { get; set; }
            public decimal Stake { get; set; }
            public decimal TrueStake { get; set; }
            public decimal Odds { get; set; }
            public int OddsTypeId { get; set; }
            public string OddsTypeName { get; set; }
            public decimal Payout { get; set; }
            public decimal WinLoss { get; set; }
            public int BetStatusId { get; set; }
            public string BetStatusName { get; set; }
            public string NewBetStatusId { get; set; } = "0";
            public string NewBetStatusName { get; set; }
            public string Reason { get; set; }
            public string NewReason { get; set; }
            public string CurrencyCode { get; set; }
            public string LanguageCode { get; set; }
            public decimal ExchangeRate { get; set; }
            public string IPAddress { get; set; }
            public string Channel { get; set; }
            public string ClientId { get; set; }
            public int ClientIdRef { get; set; }
            public string ClientCode { get; set; }
            public string ClientName { get; set; }
            public string LimitId { get; set; }
            public string Endpoint { get; set; }
            public string OperatorToken { get; set; }
            public decimal CompanyShare { get; set; }
            public bool IsResettled { get; set; }
            public bool IsCashout { get; set; }
            public DateTime BetDate { get; set; }
            public DateTime BetDateOffset { get; set; }
            public DateTime SettlementDate { get; set; }
            public DateTime SettlementDateOffset { get; set; }
            public decimal StakeTax { get; set; }
            public decimal WinningsTax { get; set; }
            public decimal TaxedStake { get; set; }
            public decimal TaxedWinnings { get; set; }
            public decimal ParlayBonus { get; set; }
            public decimal ParlayBonusWinnings { get; set; }
            public decimal StakeReal { get; set; }
            public decimal StakeBonus { get; set; }
            public decimal StakeRealPayout { get; set; }
            public decimal StakeBonusPayout { get; set; }
            public bool IsFreeBet { get; set; }
            public bool IsBoosted { get; set; }
            public List<BetSelectionDetails> Selections { get; set; }
        }
        #endregion
    }
}
