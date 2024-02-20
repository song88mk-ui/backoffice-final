using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Serilog;
using System.Text.Json;

namespace Backoffice.Web.Components.SBC.Clients
{
    public partial class CreateOrUpdateModal : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
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
        private EditContext EditContext { get; set; }
        public bool IsPreload { get; set; }
        public decimal Opacity { get; set; }

        #endregion

        #region Fields

        const decimal maxInputVal = 100;
        private decimal inputWinnings;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            IsPreload = false;
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
            monthlyFeeToggle = Client.MonthlyFee > 0 ? true : false;

            if (IsEdit)
            {
                //Tax Config
                var stakeSports = string.IsNullOrEmpty(Client.TaxConfig.ExcludeStakeSportTypes) ? new List<int>() : Client.TaxConfig.ExcludeStakeSportTypes.Split(",").Select(s => Convert.ToInt32(s)).ToList();
                var winSports = string.IsNullOrEmpty(Client.TaxConfig.ExcludeWinningsSportTypes) ? new List<int>() : Client.TaxConfig.ExcludeWinningsSportTypes.Split(",").Select(s => Convert.ToInt32(s)).ToList();

                SelectedRealSports = stakeSports.Any() ? stakeSports.Where(s => s < 1000).ToList() : new List<int>();
                SelectedVirtualSports = stakeSports.Any() ? stakeSports.Where(s => s >= 1000).ToList() : new List<int>();

                SelectedRealSportsWin = winSports.Any() ? winSports.Where(s => s < 1000).ToList() : new List<int>();
                SelectedVirtualSportsWin = winSports.Any() ? winSports.Where(s => s >= 1000).ToList() : new List<int>();

                //Revenue share sportbook
                if (Client.IsTierRevenue)
                {
                    if (!string.IsNullOrEmpty(Client.TierSportsbookShare))
                        SportsbookTier = JsonSerializer.Deserialize<TierRevenueShareModel>(Client.TierSportsbookShare) ?? new TierRevenueShareModel() { AboveTierPercentage = 0.50m };
                    if (!string.IsNullOrEmpty(Client.TierJackpotShare))
                        JackpotTier = JsonSerializer.Deserialize<TierRevenueShareModel>(Client.TierJackpotShare) ?? new TierRevenueShareModel() { AboveTierPercentage = 0.50m };

                    SportsbookTier.AboveTierPercentage = SportsbookTier.AboveTierPercentage * 100m;
                    foreach (var tier in SportsbookTier.Tiers)
                    {
                        tier.Percentage = tier.Percentage * 100m;
                    }
                    JackpotTier.AboveTierPercentage = JackpotTier.AboveTierPercentage * 100m;
                    foreach (var tier in JackpotTier.Tiers)
                    {
                        tier.Percentage = tier.Percentage * 100m;
                    }

                }
            }

            var concealedCurrency = new[] { Currency.ALL, Currency.PHP };

            Currencies = Enum.GetValues<Currency>().Except(concealedCurrency).Select(x => x.ToString()).ToList();

            if (!IsEdit)
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
                        BetSettingBetBuilder = new ClientListResponse.BetSettingBetBuilder
                        {
                            MinStake = suggestedMinimumBet,
                            MaxStake = suggestedMaximumBet,
                        },
                        BetSettingCricket = new ClientListResponse.BetSettingCricket
                        {
                            MinPerTicket = suggestedMinimumBet,
                            MaxPerTicket = suggestedMaximumBet,
                        },
                        BetSettingSoccer = new ClientListResponse.BetSettingSoccer
                        {
                            MinPerTicket = suggestedMinimumBet,
                            MaxPerTicket = suggestedMaximumBet,
                        },
                        BetSettingParlay = new ClientListResponse.BetSettingParlay
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
                                MaxPerTicket = suggestedMaximumBet,
                            },
                            BetSettingBetBuilder = new ClientListResponse.BetSettingBetBuilder
                            {
                                MinStake = suggestedMinimumBet,
                                MaxStake = suggestedMaximumBet,
                            },
                            BetSettingCricket = new ClientListResponse.BetSettingCricket
                            {
                                MinPerTicket = suggestedMinimumBet,
                                MaxPerTicket = suggestedMaximumBet,
                            },
                            BetSettingSoccer = new ClientListResponse.BetSettingSoccer
                            {
                                MinPerTicket = suggestedMinimumBet,
                                MaxPerTicket = suggestedMaximumBet,
                            },
                            BetSettingParlay = new ClientListResponse.BetSettingParlay
                            {
                                MinPerTicket = suggestedMinimumBet,
                                MaxPerTicket = suggestedMaximumBet,
                                MaxWinningsBonus = suggestedMaximumBet
                            },

                        });
                    }
                }

                foreach (var setting in Client.BetSettings)
                {
                    var suggestedMinimumBet = await GetSuggestedMinimumBet(setting.CurrencyCode);
                    var suggestedMaximumBet = suggestedMinimumBet <= 100 ? 100 : suggestedMinimumBet;

                    if (setting.BetSettingBetBuilder.MinStake == null)
                        setting.BetSettingBetBuilder.MinStake = suggestedMinimumBet;

                    if (setting.BetSettingBetBuilder.MaxStake == null)
                        setting.BetSettingBetBuilder.MaxStake = suggestedMaximumBet;

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

            EditContext = new EditContext(Client);
        }

        public async Task OnCancelClickEvent()
        {
            if (IsPreload)
                return;

            ModalService.Cancel();
        }

        public async Task OnSaveClickEvent()
        {
            try
            {
                if (IsPreload)
                    return;

                IsPreload = true;
                StateHasChanged();

                Alerts.Clear();

                Opacity = Opacity == 1m ? 0.99m : 1m;

                if (!EditContext.Validate())
                {
                    foreach (var message in EditContext.GetValidationMessages().ToList())
                    {
                        if (!Alerts.Any(a => a.Message == message))
                        {
                            Alerts.Add(new AlertModel
                            {
                                Status = AlertStatus.Error,
                                Message = $"{message}",
                                Opacity = Opacity
                            });
                        }
                    }
                    IsPreload = false;
                    return;
                }

                if (string.IsNullOrEmpty(Client.Name))
                {
                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Business Name is required",
                        Opacity = Opacity
                    });
                }

                if (string.IsNullOrEmpty(Client.ClientCode))
                {
                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Brand Name is required",
                        Opacity = Opacity
                    });
                }

                if (Alerts.Any())
                {
                    IsPreload = false;
                    return;
                }

                foreach (var setting in Client.BetSettings)
                {
                    if (setting.BetSettingSoccer.MaxPerGame > 0 && setting.BetSettingSoccer.MaxPerGame < setting.BetSettingSoccer.MaxPerTicket)
                    {
                        var message = $"MaxPerGame must be greater than or equal {setting.BetSettingSoccer.MaxPerTicket} - {setting.CurrencyCode}";
                        if (!Alerts.Any(a => a.Message == message))
                        {
                            Alerts.Add(new AlertModel
                            {
                                Status = AlertStatus.Error,
                                Message = message,
                                Opacity = Opacity
                            });
                        }
                    }

                    if (setting.BetSettingAllSports.MaxPerGame > 0 && setting.BetSettingAllSports.MaxPerGame < setting.BetSettingAllSports.MaxPerTicket)
                    {
                        var message = $"MaxPerGame must be greater than or equal {setting.BetSettingAllSports.MaxPerTicket} - {setting.CurrencyCode}";
                        if (!Alerts.Any(a => a.Message == message))
                        {
                            Alerts.Add(new AlertModel
                            {
                                Status = AlertStatus.Error,
                                Message = message,
                                Opacity = Opacity
                            });
                        }
                    }

                    if (setting.BetSettingCricket.MaxPerGame > 0 && setting.BetSettingCricket.MaxPerGame < setting.BetSettingCricket.MaxPerTicket)
                    {
                        var message = $"MaxPerGame must be greater than or equal {setting.BetSettingCricket.MaxPerTicket} - {setting.CurrencyCode}";
                        if (!Alerts.Any(a => a.Message == message))
                        {
                            Alerts.Add(new AlertModel
                            {
                                Status = AlertStatus.Error,
                                Message = message,
                                Opacity = Opacity
                            });
                        }
                    }

                }

                if (Alerts.Any())
                {
                    IsPreload = false;
                    return;
                }

                var clientRequest = new ClientListResponse.Client()
                {
                    ClientId = Client.ClientId,
                    ClientCode = Client.ClientCode ?? string.Empty,
                    Name = Client.Name ?? string.Empty,
                    Region = Client.Region ?? string.Empty,
                    Email = Client.Email ?? string.Empty,
                    IsEmailNotification = Client.IsEmailNotification,
                    MonthlyFee = Client.MonthlyFee,
                    Num = Client.Num,
                    StatusId = Client.StatusId,
                    StatusName = Client.StatusName,
                    TelephoneNumber = Client.TelephoneNumber ?? string.Empty,
                    IsTierRevenue = Client.IsTierRevenue,
                    BetSlipMaxSelection = Client.BetSlipMaxSelection,
                    TierJackpotShare = Client.TierJackpotShare,
                    TierSportsbookShare = Client.TierSportsbookShare,
                    Website = Client.Website ?? string.Empty,
                    RevenueShare = new ClientListResponse.RevenueShare
                    {
                        JackpotShare = Client.RevenueShare.JackpotShare,
                        SportsbookShare = Client.RevenueShare.SportsbookShare,
                    },
                    BetSettings = Client.BetSettings
                };

                var selectedStakeSports = new List<int>();
                if (SelectedRealSports.Any())
                    selectedStakeSports.AddRange(SelectedRealSports);

                if (SelectedVirtualSports.Any())
                    selectedStakeSports.AddRange(SelectedVirtualSports);

                var selectedWinningSports = new List<int>();
                if (SelectedRealSportsWin.Any())
                    selectedWinningSports.AddRange(SelectedRealSportsWin);

                if (SelectedVirtualSportsWin.Any())
                    selectedWinningSports.AddRange(SelectedVirtualSportsWin);

                clientRequest.TaxConfig = new ClientListResponse.TaxConfig
                {
                    ExcludeWinningsSportTypes = selectedWinningSports.Any() ? string.Join(",", selectedWinningSports) : string.Empty,
                    ExcludeStakeSportTypes = selectedStakeSports.Any() ? string.Join(",", selectedStakeSports) : string.Empty,
                    StakeTax = Client.TaxConfig.StakeTax,
                    WinningsTax = Client.TaxConfig.WinningsTax,
                };

                if (Client.TaxConfig.StakeTax > 0)
                {
                    clientRequest.TaxConfig.StakeTax = clientRequest.TaxConfig.StakeTax / 100m;
                }
                if (Client.TaxConfig.WinningsTax > 0)
                {
                    clientRequest.TaxConfig.WinningsTax = clientRequest.TaxConfig.WinningsTax / 100m;
                }

                if (clientRequest.IsTierRevenue)
                {
                    var newSportsbookTier = new TierRevenueShareModel
                    {
                        AboveTierPercentage = SportsbookTier.AboveTierPercentage / 100m
                    };
                    var newJackpotTier = new TierRevenueShareModel
                    {
                        AboveTierPercentage = JackpotTier.AboveTierPercentage / 100m
                    };

                    if (SportsbookTier.Tiers.Any())
                    {
                        newSportsbookTier.Tiers.Clear();
                        foreach (var tier in SportsbookTier.Tiers)
                        {
                            newSportsbookTier.Tiers.Add(new TierRevenueShareModel.TierModel
                            {
                                From = tier.From,
                                Tier = tier.Tier,
                                To = tier.To,
                                Percentage = tier.Percentage / 100m
                            });
                        }
                        clientRequest.TierSportsbookShare = JsonSerializer.Serialize(newSportsbookTier);
                    }


                    if (JackpotTier.Tiers.Any())
                    {
                        newJackpotTier.Tiers.Clear();
                        foreach (var tier in JackpotTier.Tiers)
                        {
                            newJackpotTier.Tiers.Add(new TierRevenueShareModel.TierModel
                            {
                                From = tier.From,
                                Tier = tier.Tier,
                                To = tier.To,
                                Percentage = tier.Percentage / 100m
                            });
                        }
                        clientRequest.TierJackpotShare = JsonSerializer.Serialize(newJackpotTier);
                    }

                }
                else
                {
                    if (clientRequest.RevenueShare.SportsbookShare > 0)
                        clientRequest.RevenueShare.SportsbookShare = clientRequest.RevenueShare.SportsbookShare / 100m;
                    if (clientRequest.RevenueShare.JackpotShare > 0)
                        clientRequest.RevenueShare.JackpotShare = clientRequest.RevenueShare.JackpotShare / 100m;

                }

                var response = (await BackofficeService.AddOrUpdateClient(clientRequest));

                if (response.Result)
                {

                    ModalService.Cancel();

                    await Modal.OnSaveClickCallback.InvokeAsync(IsEdit);
                }
                else
                {
                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = $"{response.Message}",
                        Opacity = Opacity
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }
            IsPreload = false;

        }

        public async Task OnEmailNotifClicked(ChangeEventArgs e)
        {
            if (IsPreload)
                return;

            if (Convert.ToBoolean(e.Value) == true)
                Client.IsEmailNotification = true;
            else
                Client.IsEmailNotification = false;
        }

        public async Task OnMonthlyFeeClicked(ChangeEventArgs e)
        {
            if (IsPreload)
                return;

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
            if (IsPreload)
                return;

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
            if (IsPreload)
                return;

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
            if (IsPreload)
                return;

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
            if (IsPreload)
                return;

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
            if (IsPreload)
                return;

            Client.Region = "HK";
        }

        public async Task OnRegionAFClicked(ChangeEventArgs e)
        {
            if (IsPreload)
                return;

            Client.Region = "SA";
        }
        public async Task OnStatusNormalClicked(ChangeEventArgs e)
        {
            if (IsPreload)
                return;

            Client.StatusId = (int)ClientStatus.Normal;
            Client.StatusName = ClientStatus.Normal.ToString();
        }

        public async Task OnStatusLockedClicked(ChangeEventArgs e)
        {
            if (IsPreload)
                return;

            Client.StatusId = (int)ClientStatus.Locked;
            Client.StatusName = ClientStatus.Locked.ToString();
        }

        public async Task ToggleCompanyRevenueSwitch()
        {
            if (IsPreload)
                return;

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
            if (IsPreload)
                return;

            Client.RevenueShare.SportsbookShare = Convert.ToDecimal(e.Value.ToString());
        }
        public async Task OnRevenueJackpotClicked(ChangeEventArgs e)
        {
            if (IsPreload)
                return;

            Client.RevenueShare.JackpotShare = Convert.ToDecimal(e.Value.ToString());
        }

        public async Task OnSportsbookTierOptionClicked(ChangeEventArgs e)
        {
            if (IsPreload)
                return;

            SportsbookTierOption = true;
        }

        public async Task OnJackpotTierOptionClicked(ChangeEventArgs e)
        {
            if (IsPreload)
                return;

            SportsbookTierOption = false;
        }

        public async Task OnAddTierClicked()
        {
            if (IsPreload)
                return;

            if (SportsbookTierOption)
            {
                var fromTier = 0.01m;
                if (SportsbookTier.Tiers.Any())
                {
                    var lastTier = SportsbookTier.Tiers.LastOrDefault();
                    if (lastTier != null)
                    {
                        fromTier = (lastTier.To > lastTier.From ? lastTier.To : lastTier.From) + 0.01m;
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
                        fromTier = (lastTier.To > lastTier.From ? lastTier.To : lastTier.From) + 0.01m;
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
            if (IsPreload)
                return;

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
            if (IsPreload)
                return;

            tier.Percentage = Convert.ToDecimal(e.Value.ToString());
        }
        public async Task OnAboveTierSelect(ChangeEventArgs e)
        {
            if (IsPreload)
                return;

            if (SportsbookTierOption)
            {
                SportsbookTier.AboveTierPercentage = Convert.ToDecimal(e.Value.ToString());
            }
            else
            {
                JackpotTier.AboveTierPercentage = Convert.ToDecimal(e.Value.ToString());
            }
        }
        public async Task OnCurrencyClicked(string currency)
        {
            if (IsPreload)
                return;

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
            var currency = (Currency)Enum.Parse(typeof(Currency), currencyCode.ToUpper());

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
