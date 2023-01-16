using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net;
using System.Reflection.Emit;

namespace Backoffice.Web.Components.Clients
{
    public partial class CreateOrUpdateModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        public ClientListResponse.Client Client { get; set; }
        public SportsLookupModel RealSports { get; set; }
        public SportsLookupModel VirtualSports { get; set; }
        public bool monthlyFeeToggle { get; set; }
        public List<string> Currencies { get; set; }
        public List<int> SelectedRealSports { get; set; }
        public List<int> SelectedVirtualSports { get; set; }
        public List<int> SelectedRealSportsWin { get; set; }
        public List<int> SelectedVirtualSportsWin { get; set; }
        public TierRevenueShareModel SportsbookTier { get; set; }
        public TierRevenueShareModel JackpotTier { get; set; }
        public bool SportsbookTierOption { get; set; }
        public bool IsEdit { get; set; }
        public string SelectedCurrency { get; set; }
        public ClientListResponse.ClientBetSetting SelectedBetSetting { get; set; }
        public List<AlertModel> Alerts { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            monthlyFeeToggle = false;
            SportsbookTierOption = true;

            SelectedRealSports = new List<int>();
            SelectedVirtualSports = new List<int>();
            SelectedRealSportsWin = new List<int>();
            SelectedVirtualSportsWin = new List<int>();

            SportsbookTier = new TierRevenueShareModel();
            SportsbookTier.AboveTierPercentage = 50;
            JackpotTier = new TierRevenueShareModel();
            JackpotTier.AboveTierPercentage = 50;

            Alerts = new List<AlertModel>();
            Client = ModalParameters.TryGet<ClientListResponse.Client>("Client");
            RealSports = ModalParameters.TryGet<SportsLookupModel>("RealSports");
            VirtualSports = ModalParameters.TryGet<SportsLookupModel>("VirtualSports");
            IsEdit = ModalParameters.TryGet<bool>("IsEdit");
            Client.StatusId = IsEdit ? Client.StatusId : (int)ClientStatus.Normal;
            Client.StatusName = IsEdit ? Client.StatusName : ClientStatus.Normal.ToString();
            Client.Region = IsEdit ? Client.Region : "HK";
            Client.IsTierRevenue = IsEdit ? Client.IsTierRevenue : false;

            Currencies = new List<string>()
            {
                "USD", "EUR", "GBP", "KRW", "VND", "THB", "CNY", "MYR", "KHR", "IDR", "INR", "JPY",
                "MMK", "HKD", "AED", "TRY", "TWD", "NZD", "ZAR", "KES", "TZS", "GHS", "ZMW", "MZN",
                "CDF", "ETB", "MWK", "XAF", "NGN", "BTC", "ETH", "PKR", "BRL", "CLP", "PEN", "MXN"
            };


            if(!IsEdit)
            {
                Client.BetSettings = new List<ClientListResponse.ClientBetSetting>();
                foreach (var currency in Currencies)
                {
                    Client.BetSettings.Add(new ClientListResponse.ClientBetSetting
                    {
                        CurrencyCode = currency,
                    });
                }
            }
            else
            {
                var existingCurrencies = Client.BetSettings.Select(s => s.CurrencyCode).ToList();
                foreach (var currency in Currencies)
                {
                    if(!existingCurrencies.Contains(currency))
                    {
                        Client.BetSettings.Add(new ClientListResponse.ClientBetSetting
                        {
                            CurrencyCode = currency,
                        });
                    }
                }
            }

            SelectedCurrency = Currencies.FirstOrDefault() ?? string.Empty;
            SelectedBetSetting = Client.BetSettings.FirstOrDefault(b => b.CurrencyCode == SelectedCurrency);

        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnSaveClickEvent()
        {
            try
            {
                Alerts.Clear();

                if (string.IsNullOrEmpty(Client.Name))
                {
                    Alerts.Add(new AlertModel
                    {
                        AlertStatus = "Error",
                        AltertMessage = "Business Name is required",
                        Opacity = 1.0m
                    });
                }

                if (string.IsNullOrEmpty(Client.ClientCode))
                {
                    Alerts.Add(new AlertModel
                    {
                        AlertStatus = "Error",
                        AltertMessage = "Brand Name is required",
                        Opacity = 1.0m
                    });
                }

            }
            catch (Exception ex)
            {

            }

        }

        public async Task OnEmailNotifClicked(ChangeEventArgs e)
        {
            if (Convert.ToBoolean(e.Value) == true)
                Client.IsEmailNotification = true;
            else
                Client.IsEmailNotification = false;
        }

        public async Task OnMonthlyFeeClicked(ChangeEventArgs e)
        {
            if (Convert.ToBoolean(e.Value) == true)
                monthlyFeeToggle = true;
            else
            {
                Client.MonthlyFee = 0;
                monthlyFeeToggle = false;
            }
        }

        public async Task OnRealSportsClicked(ChangeEventArgs e, int sportId, bool isLabel = false)
        {
            if ((isLabel && !SelectedRealSportsWin.Contains(sportId)) || (e != null && Convert.ToBoolean(e.Value) == true))
            {
                if (sportId == 0)
                {
                    SelectedRealSports.Clear();
                    SelectedRealSports.AddRange(RealSports.SportLookup.Select(s => s.SportId).ToList());
                }
                else
                {
                    if (!SelectedRealSports.Any(s => s == sportId))
                        SelectedRealSports.Add(sportId);
                }
            }
            else
            {
                if (sportId == 0)
                {
                    SelectedRealSports.Clear();
                }
                else
                {
                    SelectedRealSports.Remove(sportId);
                }
            }
        }
        public async Task OnVirtualSportsClicked(ChangeEventArgs e, int sportId, bool isLabel = false)
        {
            if ((isLabel && !SelectedVirtualSports.Contains(sportId)) || (e != null && Convert.ToBoolean(e.Value) == true))
            {
                if (sportId == 0)
                {
                    SelectedVirtualSports.Clear();
                    SelectedVirtualSports.AddRange(VirtualSports.SportLookup.Select(s => s.SportId).ToList());
                }
                else
                {
                    if (!SelectedVirtualSports.Any(s => s == sportId))
                        SelectedVirtualSports.Add(sportId);
                }
            }
            else
            {
                if (sportId == 0)
                {
                    SelectedVirtualSports.Clear();
                }
                else
                {
                    SelectedVirtualSports.Remove(sportId);
                }
            }
        }
        public async Task OnRealSportsWinClicked(ChangeEventArgs e, int sportId, bool isLabel = false)
        {
            if ((isLabel && !SelectedRealSportsWin.Contains(sportId)) || (e != null && Convert.ToBoolean(e.Value) == true))
            {
                if (sportId == 0)
                {
                    SelectedRealSportsWin.Clear();
                    SelectedRealSportsWin.AddRange(RealSports.SportLookup.Select(s => s.SportId).ToList());
                }
                else
                {
                    if (!SelectedRealSportsWin.Any(s => s == sportId))
                        SelectedRealSportsWin.Add(sportId);
                }
            }
            else
            {
                if (sportId == 0)
                {
                    SelectedRealSportsWin.Clear();
                }
                else
                {
                    SelectedRealSportsWin.Remove(sportId);
                }
            }
        }
        public async Task OnVirtualSportsWinClicked(ChangeEventArgs e, int sportId, bool isLabel = false)
        {
            if ((isLabel && !SelectedRealSportsWin.Contains(sportId)) || (e != null && Convert.ToBoolean(e.Value) == true))
            {
                if (sportId == 0)
                {
                    SelectedVirtualSportsWin.Clear();
                    SelectedVirtualSportsWin.AddRange(VirtualSports.SportLookup.Select(s => s.SportId).ToList());
                }
                else
                {
                    if (!SelectedVirtualSportsWin.Any(s => s == sportId))
                        SelectedVirtualSportsWin.Add(sportId);
                }
            }
            else
            {
                if (sportId == 0)
                {
                    SelectedVirtualSportsWin.Clear();
                }
                else
                {
                    SelectedVirtualSportsWin.Remove(sportId);
                }
            }
        }

        public async Task OnRegionHKClicked(ChangeEventArgs e)
        {
            Client.Region = "HK";
        }

        public async Task OnRegionAFClicked(ChangeEventArgs e)
        {
            Client.Region = "SA";
        }

        public async Task ToggleCompanyRevenueSwitch()
        {
            //false = fixed
            //true = tier
            Client.IsTierRevenue = !Client.IsTierRevenue;

            if (Client.IsTierRevenue)
            {
                Client.RevenueShare = new ClientListResponse.RevenueShare();
            }
            else
            {
                Client.TierSportsbookShare = string.Empty;
                Client.TierJackpotShare = string.Empty;

                SportsbookTier = new TierRevenueShareModel();
                SportsbookTier.AboveTierPercentage = 50;
                JackpotTier = new TierRevenueShareModel();
                JackpotTier.AboveTierPercentage = 50;
            }
        }

        public async Task OnRevenueSportsbookClicked(ChangeEventArgs e)
        {
            Client.RevenueShare.SportsbookShare = Convert.ToDecimal(e.Value.ToString());
        }
        public async Task OnRevenueJackpotClicked(ChangeEventArgs e)
        {
            Client.RevenueShare.JackpotShare = Convert.ToDecimal(e.Value.ToString());
        }

        public async Task OnSportsbookTierOptionClicked(ChangeEventArgs e)
        {
            SportsbookTierOption = true;
        }

        public async Task OnJackpotTierOptionClicked(ChangeEventArgs e)
        {
            SportsbookTierOption = false;
        }

        public async Task OnAddTierClicked()
        {
            if (SportsbookTierOption)
            {
                var fromTier = 0.01m;
                if (SportsbookTier.Tiers.Any())
                {
                    var lastTier = SportsbookTier.Tiers.LastOrDefault();
                    if (lastTier != null)
                    {
                        fromTier = lastTier.To > lastTier.From ? lastTier.To : lastTier.From;
                    }

                }
                SportsbookTier.Tiers.Add(new TierRevenueShareModel.TierModel
                {
                    Tier = SportsbookTier.Tiers.Count + 1,
                    From = fromTier,
                    To = fromTier,
                    Percentage = 100
                });

            }
            else
            {
                var fromTier = 0.01m;
                if (JackpotTier.Tiers.Any())
                {
                    var lastTier = JackpotTier.Tiers.LastOrDefault();
                    if (lastTier != null)
                    {
                        fromTier = lastTier.To > lastTier.From ? lastTier.To : lastTier.From;
                    }

                }
                JackpotTier.Tiers.Add(new TierRevenueShareModel.TierModel
                {
                    Tier = JackpotTier.Tiers.Count + 1,
                    From = fromTier,
                    To = fromTier,
                    Percentage = 100
                });
            }
        }

        public async Task OnRemoveTierClicked(TierRevenueShareModel.TierModel tier)
        {
            var num = tier.Tier;

            if (SportsbookTierOption)
            {
                SportsbookTier.Tiers.Remove(tier);

                for (int i = num - 1; i < SportsbookTier.Tiers.Count; i++)
                {
                    SportsbookTier.Tiers[i].Tier = i + 1;
                }
            }
            else
            {
                JackpotTier.Tiers.Remove(tier);

                for (int i = num - 1; i < JackpotTier.Tiers.Count; i++)
                {
                    JackpotTier.Tiers[i].Tier = i + 1;
                }

            }
        }

        public async Task OnTierSelect(ChangeEventArgs e, TierRevenueShareModel.TierModel tier)
        {
            tier.Percentage = Convert.ToDecimal(e.Value.ToString());
        }
        public async Task OnCurrencyClicked(string currency)
        {
            SelectedCurrency = currency;
            SelectedBetSetting = Client.BetSettings.FirstOrDefault(b => b.CurrencyCode == SelectedCurrency);
        }


        public void Dispose()
        {

        }

        #endregion
    }
}
