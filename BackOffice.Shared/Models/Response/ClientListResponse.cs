using System.Collections.Generic;
using System;

namespace Backoffice.Shared.Models.Response
{
    public class ClientListResponse
    {
        public ClientListResponse()
        {
            Clients = new List<Client>();
        }

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPage { get; set; }
        public List<Client> Clients { get; set; }

        #region Nested Class

        public class Client
        {
            public Client()
            {
                BetSettings = new List<ClientBetSetting>();
            }
            public int Num { get; set; }
            public int ClientId { get; set; }
            public string ClientCode { get; set; }
            public string Name { get; set; }
            public string Website { get; set; }
            public string TelephoneNumber { get; set; }
            public int StatusId { get; set; }
            public string StatusName { get; set; }
            public string Email { get; set; }
            public bool IsEmailNotification { get; set; }
            public string Region { get; set; }
            public decimal MonthlyFee { get; set; }
            public TaxConfig TaxConfig { get; set; }
            public bool IsTierRevenue { get; set; }
            public RevenueShare RevenueShare { get; set; }
            public string TierSportsbookShare { get; set; }
            public string TierJackpotShare { get; set; }
            public int BetSlipSelectionCount { get; set; }
            public List<ClientBetSetting> BetSettings { get; set; }
        }

        public class ClientBetSetting
        {
            public string CurrencyCode { get; set; }
            public BetSettingSoccer BetSettingSoccer { get; set; }
            public BetSettingAllSports BetSettingAllSports { get; set; }
            public BetSettingParlay BetSettingParlay { get; set; }
            public BetSettingCricket BetSettingCricket { get; set; }
            public BetSettingPriceboost BetSettingPriceboost { get; set; }
            public BetSettingBetBuilder BetSettingBetBuilder { get; set; }
        }

        public class RevenueShare
        {
            public decimal SportsbookShare { get; set; }
            public decimal JackpotShare { get; set; }
        }

        public class TaxConfig
        {
            public decimal StakeTax { get; set; }
            public decimal WinningsTax { get; set; }
            public string ExcludeStakeSportTypes { get; set; }
            public string ExcludeWinningsSportTypes { get; set; }
        }


        public class BetSettingSoccer
        {
            public decimal MinPerTicket { get; set; }
            public decimal MaxPerTicket { get; set; }
            public decimal MaxPerGame { get; set; }
        }

        public class BetSettingAllSports
        {
            public decimal MinPerTicket { get; set; }
            public decimal MaxPerTicket { get; set; }
            public decimal MaxPerGame { get; set; }
        }

        public class BetSettingParlay
        {
            public decimal MinPerTicket { get; set; }
            public decimal MaxPerTicket { get; set; }
            public decimal MaxPayoutPerTicket { get; set; }
            public decimal MaxWinningsBonus { get; set; }
        }

        public class BetSettingCricket
        {
            public decimal MinPerTicket { get; set; }
            public decimal MaxPerTicket { get; set; }
            public decimal MaxPerGame { get; set; }
        }

        public class BetSettingPriceboost
        {
            public decimal MaxStake { get; set; }
        }

        public class BetSettingBetBuilder
        {
            public decimal MinStake { get; set; }
            public decimal MaxStake { get; set; }
        }
        #endregion
    }
}
