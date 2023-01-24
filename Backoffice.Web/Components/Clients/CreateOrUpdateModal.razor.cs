using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using static Backoffice.Shared.Models.Response.ClientListResponse;

namespace Backoffice.Web.Components.Clients
{
    public partial class CreateOrUpdateModal : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
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
        public ClientListResponse.BetSetting SelectedBetSetting { get; set; }
        public List<AlertModel> Alerts { get; set; }

        #endregion

        #region Fields
        const decimal maxInputVal = 100;
        private decimal? InputStake { get; set; }
        private decimal inputWinnings;
        private decimal InputWinnings
        {
            get => inputWinnings;
            set => inputWinnings = Math.Min(value, maxInputVal);
        }
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
            SportsbookTier.AboveTierPercentage = 50m;
            JackpotTier = new TierRevenueShareModel();
            JackpotTier.AboveTierPercentage = 50m;

            Alerts = new List<AlertModel>();
            Client = ModalParameters.TryGet<ClientListResponse.Client>("Client");
            RealSports = ModalParameters.TryGet<SportsLookupModel>("RealSports");
            VirtualSports = ModalParameters.TryGet<SportsLookupModel>("VirtualSports");
            IsEdit = ModalParameters.TryGet<bool>("IsEdit");
            Client.StatusId = IsEdit ? Client.StatusId : (int)ClientStatus.Normal;
            Client.StatusName = IsEdit ? Client.StatusName : ClientStatus.Normal.ToString();
            Client.Region = IsEdit ? Client.Region : "HK";
            Client.IsTierRevenue = IsEdit ? Client.IsTierRevenue : false;
            Client.MonthlyFee = Convert.ToDecimal(Client.MonthlyFee.ToString("0.00"));

            if(IsEdit)
            {
                //Tax Config
                var stakeSports = string.IsNullOrEmpty(Client.TaxConfig.ExcludeStakeSportTypes) ? new List<int>() : Client.TaxConfig.ExcludeStakeSportTypes.Split(",").Select(s => Convert.ToInt32(s)).ToList();
                var winSports = string.IsNullOrEmpty(Client.TaxConfig.ExcludeWinningsSportTypes) ? new List<int>() : Client.TaxConfig.ExcludeWinningsSportTypes.Split(",").Select(s => Convert.ToInt32(s)).ToList();

                SelectedRealSports = stakeSports.Any() ? stakeSports.Where(s => s < 1000).ToList() : new List<int>();
                SelectedVirtualSports = stakeSports.Any() ? stakeSports.Where(s => s >= 1000).ToList() : new List<int>();

                SelectedRealSportsWin = winSports.Any() ? winSports.Where(s => s < 1000).ToList() : new List<int>();
                SelectedVirtualSportsWin = winSports.Any() ? winSports.Where(s => s >= 1000).ToList() : new List<int>();

                //Revenue share sportbook
                if(Client.IsTierRevenue)
                {
                    if (!string.IsNullOrEmpty(Client.TierSportsbookShare))
                        SportsbookTier = JsonSerializer.Deserialize<TierRevenueShareModel>(Client.TierSportsbookShare) ?? new TierRevenueShareModel() { AboveTierPercentage = 50m };
                    if (!string.IsNullOrEmpty(Client.TierJackpotShare))
                        JackpotTier = JsonSerializer.Deserialize<TierRevenueShareModel>(Client.TierJackpotShare) ?? new TierRevenueShareModel() { AboveTierPercentage = 50m };
                }
            }

            Currencies = new List<string>()
            {
                "USD", "EUR", "GBP", "KRW", "VND", "THB", "CNY", "MYR", "KHR", "IDR", "INR", "JPY",
                "MMK", "HKD", "AED", "TRY", "TWD", "NZD", "ZAR", "KES", "TZS", "GHS", "ZMW", "MZN",
                "CDF", "ETB", "MWK", "XAF", "NGN", "BTC", "ETH", "PKR", "BRL", "CLP", "PEN", "MXN"
            };


            if(!IsEdit)
            {
                Client.BetSettings = new List<ClientListResponse.BetSetting>();
                foreach (var currency in Currencies)
                {
                    decimal suggestedMinimumBet = await GetSuggestedMinimumBet(currency);
                    decimal suggestedMaximumBet = suggestedMinimumBet <= 100 ? 100 : suggestedMinimumBet;

                    Client.BetSettings.Add(new ClientListResponse.BetSetting
                    {
                        CurrencyCode = currency,
                        BetSettingAllSports = new ClientListResponse.BetSettingAllSports
                        {
                            MinPerTicket = suggestedMinimumBet,
                            MaxPerTicket = suggestedMaximumBet,
                        },
                        BetSettingBetBuilder = new BetSettingBetBuilder
                        {
                            MinStake = suggestedMinimumBet,
                            MaxStake = suggestedMaximumBet,
                        },
                        BetSettingCricket = new BetSettingCricket
                        {
                            MinPerTicket = suggestedMinimumBet,
                            MaxPerTicket = suggestedMaximumBet,
                        },
                        BetSettingSoccer = new BetSettingSoccer
                        {
                            MinPerTicket = suggestedMinimumBet,
                            MaxPerTicket = suggestedMaximumBet,
                        },
                        BetSettingParlay = new BetSettingParlay
                        {
                            MinPerTicket = suggestedMinimumBet,
                            MaxPerTicket = suggestedMaximumBet,
                            MaxWinningsBonus = suggestedMaximumBet
                        }
                    });
                }
            }
            else
            {

                var existingCurrencies = Client.BetSettings.Select(s => s.CurrencyCode).ToList();
                foreach (var currency in Currencies)
                {
                    decimal suggestedMinimumBet = await GetSuggestedMinimumBet(currency);
                    decimal suggestedMaximumBet = suggestedMinimumBet <= 100 ? 100 : suggestedMinimumBet;
                    if (!existingCurrencies.Contains(currency))
                    {
                        Client.BetSettings.Add(new ClientListResponse.BetSetting
                        {
                            CurrencyCode = currency,
                            BetSettingAllSports = new ClientListResponse.BetSettingAllSports
                            {
                                MinPerTicket = suggestedMinimumBet,
                                MaxPerTicket= suggestedMaximumBet,
                            },
                            BetSettingBetBuilder = new BetSettingBetBuilder
                            {
                                MinStake = suggestedMinimumBet,
                                MaxStake = suggestedMaximumBet,
                            },
                            BetSettingCricket = new BetSettingCricket
                            {
                                MinPerTicket = suggestedMinimumBet,
                                MaxPerTicket = suggestedMaximumBet,
                            },
                            BetSettingSoccer = new BetSettingSoccer
                            {
                                MinPerTicket = suggestedMinimumBet,
                                MaxPerTicket = suggestedMaximumBet,
                            },
                            BetSettingParlay = new BetSettingParlay
                            {
                                MinPerTicket = suggestedMinimumBet,
                                MaxPerTicket = suggestedMaximumBet,
                                MaxWinningsBonus = suggestedMaximumBet
                            },

                        });
                    }
                }

                foreach(var setting in Client.BetSettings )
                {
                    decimal suggestedMinimumBet = await GetSuggestedMinimumBet(setting.CurrencyCode);
                    decimal suggestedMaximumBet = suggestedMinimumBet <= 100 ? 100 : suggestedMinimumBet;

                    if (setting.BetSettingBetBuilder.MinStake == 0)
                        setting.BetSettingBetBuilder.MinStake = suggestedMinimumBet;

                    if (setting.BetSettingBetBuilder.MaxStake == 0)
                        setting.BetSettingBetBuilder.MaxStake = suggestedMaximumBet;

                    if (setting.BetSettingAllSports.MinPerTicket == 0)
                        setting.BetSettingAllSports.MinPerTicket = suggestedMinimumBet;

                    if (setting.BetSettingAllSports.MaxPerTicket == 0)
                        setting.BetSettingAllSports.MaxPerTicket = suggestedMaximumBet;

                    if (setting.BetSettingSoccer.MinPerTicket == 0)
                        setting.BetSettingSoccer.MinPerTicket = suggestedMinimumBet;

                    if (setting.BetSettingSoccer.MaxPerTicket == 0)
                        setting.BetSettingSoccer.MaxPerTicket = suggestedMaximumBet;

                    if (setting.BetSettingCricket.MinPerTicket == 0)
                        setting.BetSettingCricket.MinPerTicket = suggestedMinimumBet;

                    if (setting.BetSettingCricket.MaxPerTicket == 0)
                        setting.BetSettingCricket.MaxPerTicket = suggestedMaximumBet;

                    if (setting.BetSettingParlay.MinPerTicket == 0)
                        setting.BetSettingParlay.MinPerTicket = suggestedMinimumBet;

                    if (setting.BetSettingParlay.MaxPerTicket == 0)
                        setting.BetSettingParlay.MaxPerTicket = suggestedMaximumBet;

                    if (setting.BetSettingParlay.MaxWinningsBonus == 0)
                        setting.BetSettingParlay.MaxWinningsBonus = suggestedMaximumBet;
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

                if (Alerts.Any())
                    return;

                var clientRequest = new ClientListResponse.Client()
                {
                    ClientId = Client.ClientId,
                    ClientCode = Client.ClientCode ?? string.Empty,
                    Name= Client.Name ?? string.Empty,
                    Region= Client.Region ?? string.Empty,
                    Email= Client.Email ?? string.Empty,
                    IsEmailNotification = Client.IsEmailNotification,
                    MonthlyFee = Client.MonthlyFee,
                    Num = Client.Num,
                    StatusId= Client.StatusId,
                    StatusName= Client.StatusName,
                    TelephoneNumber= Client.TelephoneNumber ?? string.Empty,
                    IsTierRevenue = Client.IsTierRevenue,
                    BetSlipMaxSelection = Client.BetSlipMaxSelection,
                    TierJackpotShare= Client.TierJackpotShare,
                    TierSportsbookShare = Client.TierSportsbookShare,
                    Website = Client.Website ?? string.Empty,
                    RevenueShare = new RevenueShare
                    {
                        JackpotShare = Client.RevenueShare.JackpotShare,
                        SportsbookShare = Client.RevenueShare.SportsbookShare,
                    },
                    BetSettings = Client.BetSettings
                };

                var selectedStakeSports = new List<int>();
                if(SelectedRealSports.Any())
                    selectedStakeSports.AddRange(SelectedRealSports);

                if (SelectedVirtualSports.Any())
                    selectedStakeSports.AddRange(SelectedVirtualSports);

                var selectedWinningSports = new List<int>();
                if (SelectedRealSportsWin.Any())
                    selectedWinningSports.AddRange(SelectedRealSportsWin);

                if (SelectedVirtualSportsWin.Any())
                    selectedWinningSports.AddRange(SelectedVirtualSportsWin);

                clientRequest.TaxConfig = new TaxConfig
                {
                    ExcludeWinningsSportTypes = selectedWinningSports.Any() ? string.Join(",", selectedWinningSports) : string.Empty,
                    ExcludeStakeSportTypes = selectedStakeSports.Any() ? string.Join(",", selectedStakeSports) : string.Empty,
                    StakeTax = Client.TaxConfig.StakeTax,
                    WinningsTax = Client.TaxConfig.WinningsTax,
                };
                
                if(Client.TaxConfig.StakeTax > 0)
                {
                    clientRequest.TaxConfig.StakeTax = clientRequest.TaxConfig.StakeTax / 100m;
                }
                if (Client.TaxConfig.WinningsTax > 0)
                {
                    clientRequest.TaxConfig.WinningsTax = clientRequest.TaxConfig.WinningsTax / 100m;
                }

                if(clientRequest.IsTierRevenue)
                {
                    if(SportsbookTier.Tiers.Any())
                        foreach(var tier in SportsbookTier.Tiers)
                        {
                            tier.Percentage = tier.Percentage / 100m;
                        }

                    if (JackpotTier.Tiers.Any())
                        foreach (var tier in JackpotTier.Tiers)
                        {
                            tier.Percentage = tier.Percentage / 100m;
                        }

                    clientRequest.TierSportsbookShare = JsonSerializer.Serialize(SportsbookTier);
                    clientRequest.TierJackpotShare = JsonSerializer.Serialize(JackpotTier);
                }
                else
                {
                    if (clientRequest.RevenueShare.SportsbookShare > 0)
                        clientRequest.RevenueShare.SportsbookShare = clientRequest.RevenueShare.SportsbookShare / 100m;
                    if (clientRequest.RevenueShare.JackpotShare > 0)
                        clientRequest.RevenueShare.JackpotShare = clientRequest.RevenueShare.JackpotShare / 100m;

                }

                var response = (await BackOfficeService.AddOrUpdateClient(clientRequest));

                if (response.Result)
                {
                    if(IsEdit)
                    {
                        Alerts.Add(new AlertModel
                        {
                            AlertStatus = "Success",
                            AltertMessage = "Client successfully updated",
                            Opacity = 1.0m
                        });
                    }
                    else
                    {
                        Alerts.Add(new AlertModel
                        {
                            AlertStatus = "Success",
                            AltertMessage = "Client successfully created",
                            Opacity = 1.0m
                        });
                    }


                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(3500);

                    ModalService.Cancel();

                    await Modal.OnSaveClickCallback.InvokeAsync();
                }
                else
                {
                    Alerts.Add(new AlertModel
                    {
                        AlertStatus = "Error",
                        AltertMessage = $"{response.Message}",
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
        public async Task OnStatusNormalClicked(ChangeEventArgs e)
        {
            Client.StatusId = (int)ClientStatus.Normal;
            Client.StatusName = ClientStatus.Normal.ToString();
        }

        public async Task OnStatusLockedClicked(ChangeEventArgs e)
        {
            Client.StatusId = (int)ClientStatus.Locked;
            Client.StatusName = ClientStatus.Locked.ToString();
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

        #region Utilities
        private async Task<decimal> GetSuggestedMinimumBet(string currencyCode)
        {
            var currency = (Currency)Enum.Parse(typeof(Currency), currencyCode);

            var suggestedMinimumBet = 1m;

            var highValueArr = new Currency[] { Currency.KRW, Currency.VND, Currency.KHR, Currency.IDR, Currency.MMK };
            var minValue100Arr = new Currency[] { Currency.CDF, Currency.TZS };
            var minValue1Arr = new Currency[] { Currency.PKR };

            if (highValueArr.Contains(currency))
            {
                suggestedMinimumBet = 1000m;
            }
            else if (minValue100Arr.Contains(currency))
            {
                suggestedMinimumBet = 100m;
            }
            else if (currency == Currency.BTC || currency == Currency.ETH)
            {
                //suggestedMinimumBet = 0.000000010000m;
                suggestedMinimumBet = 0.00010000m; // FCI-6803
            }
            else if (minValue1Arr.Contains(currency))
            {
                suggestedMinimumBet = 1m;
            }
            else
            {
                suggestedMinimumBet = 0.02m;
            }

            return suggestedMinimumBet;
        }
        #endregion
    }
}
