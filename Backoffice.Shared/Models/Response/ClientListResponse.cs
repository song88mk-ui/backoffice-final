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

        #region Nested
        public class Client
        {
            public Client()
            {
                BetSettings = new List<BetSetting>();
                RevenueShare = new RevenueShare();
                TaxConfig = new TaxConfig();
            }
            public int Num { get; set; }
            public int ClientId { get; set; }

            public System.Guid? PlatformClientId { get; set; }

            // Input only: the Client ID Platform should create a NEW client with (null = Platform generates).
            // Kept separate from PlatformClientId, which AddOrUpdateClient sets only once Platform succeeds.
            public System.Guid? RequestedPlatformClientId { get; set; }
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
            public string LimitId { get; set; }
            public string Endpoint { get; set; }
            public TaxConfig TaxConfig { get; set; }
            public bool IsTierRevenue { get; set; }
            public RevenueShare RevenueShare { get; set; }
            public string TierSportsbookShare { get; set; }
            public string TierJackpotShare { get; set; }
            public int BetSlipMaxSelection { get; set; }
            public List<BetSetting> BetSettings { get; set; }
        }

        public class BetSetting
        {
            public BetSetting()
            {
                BetSettingSoccer = new BetSettingSoccer();
                BetSettingAllSports = new BetSettingAllSports();
                BetSettingParlay = new BetSettingParlay();
                BetSettingCricket = new BetSettingCricket();
                BetSettingPriceboost = new BetSettingPriceboost();
                BetSettingBetBuilder = new BetSettingBetBuilder();
            }
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
            public decimal? StakeTax { get; set; } = 0m;
            public decimal? WinningsTax { get; set; } = 0m;
            public string ExcludeStakeSportTypes { get; set; }
            public string ExcludeWinningsSportTypes { get; set; }
        }


        public class BetSettingSoccer
        {
            public decimal? MinPerTicket { get; set; }
            public decimal? MaxPerTicket { get; set; }
            public decimal? MaxPerGame { get; set; }
        }

        public class BetSettingAllSports
        {
            public decimal? MinPerTicket { get; set; }
            public decimal? MaxPerTicket { get; set; }
            public decimal? MaxPerGame { get; set; }
        }

        public class BetSettingParlay
        {
            public decimal? MinPerTicket { get; set; }
            public decimal? MaxPerTicket { get; set; }
            public decimal? MaxPayoutPerTicket { get; set; }
            public decimal? MaxWinningsBonus { get; set; }
        }

        public class BetSettingCricket
        {
            public decimal? MinPerTicket { get; set; }
            public decimal? MaxPerTicket { get; set; }
            public decimal? MaxPerGame { get; set; }
        }

        public class BetSettingPriceboost
        {
            public decimal? MaxStake { get; set; }
        }

        public class BetSettingBetBuilder
        {
            public decimal? MinStake { get; set; }
            public decimal? MaxStake { get; set; }
        }
        #endregion
    }
}
