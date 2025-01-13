using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;

namespace Backoffice.Web.Components.Administration.ManualBetSettlement
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackofficeService BackofficeService { get; set; }

        [Parameter] public EventCallback OnSearchButtonCallback { get; set; }
        [Parameter] public EventCallback<ManualBetSettlementCallbackModel> OnInitCallback { get; set; }
        [Parameter] public EventCallback OnExportToExcelCallback { get; set; }
        [Parameter] public bool IsExportDisabled { get; set; }
        [Parameter] public EventCallback<bool> IsExportDisabledChanged { get; set; }

        public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Sports { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Currencies { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> FromStatuses { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> ToStatuses { get; set; }
        public ManualBetSettlementCallbackModel CallbackModel { get; set; }

        public string SelectedClientName { get; set; }
        public int SelectedClientCount { get; set; }
        public string SelectedSportName { get; set; }
        public int SelectedSportCount { get; set; }
        public string SelectedCurrencyName { get; set; }
        public int SelectedCurrencyCount { get; set; }
        public string SelectedFromStatusName { get; set; }
        public int SelectedFromStatusCount { get; set; }
        public string SelectedToStatusName { get; set; }
        public int SelectedToStatusCount { get; set; }
        public bool ShowFilter { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ShowFilter = false;

            CallbackModel = new ManualBetSettlementCallbackModel();

            Clients = new List<SelectListItemWithCheckboxModel<string>>();
            Sports = new List<SelectListItemWithCheckboxModel<string>>();
            Currencies = new List<SelectListItemWithCheckboxModel<string>>();
            FromStatuses = new List<SelectListItemWithCheckboxModel<string>>();
            ToStatuses = new List<SelectListItemWithCheckboxModel<string>>();

            await LoadClients();
            await LoadSports();
            await LoadCurrencies();
            await LoadFromStatus();
            await LoadToStatus();

            await OnInitCallback.InvokeAsync(CallbackModel);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            //if (firstRender)
            //{
            //    ShowFilter = false;

            //    CallbackModel = new ManualBetSettlementCallbackModel();

            //    Clients = new List<SelectListItemWithCheckboxModel<string>>();
            //    Sports = new List<SelectListItemWithCheckboxModel<string>>();
            //    Currencies = new List<SelectListItemWithCheckboxModel<string>>();
            //    FromStatuses = new List<SelectListItemWithCheckboxModel<string>>();
            //    ToStatuses = new List<SelectListItemWithCheckboxModel<string>>();

            //    await LoadClients();
            //    await LoadSports();
            //    await LoadCurrencies();
            //    await LoadFromStatus();
            //    await LoadToStatus();

            //    await OnInitCallback.InvokeAsync(CallbackModel);
            //}
        }

        public async Task OnAddReferenceClickEvent()
        {
            ModalParameters parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 80rem");

            ModalOptions options = new ModalOptions();

            ModalService.Open<AddReferenceModal>("Add Reference ID", parameters, options);
        }

        public async Task OnExportToExcelClickEvent()
        {
            await OnExportToExcelCallback.InvokeAsync();
        }

        public async Task OnFilterClickEvent()
        {
            ShowFilter = !ShowFilter;
        }

        public async Task OnClientCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedClientName = string.Empty;
            SelectedClientCount = items.Count;

            if (SelectedClientCount == 1)
            {
                SelectedClientName = items.FirstOrDefault().Value;
            }

            CallbackModel.Clients = Clients.Where(c => c.Value != "all" && c.IsChecked).Select(s => new ManualBetSettlementCallbackModel.SettlementClient
            {
                ClientId = long.Parse(s.Value),
                ClientName = s.Name
            }).ToList();

            await OnInitCallback.InvokeAsync(CallbackModel);
        }

        public async Task OnSportCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedSportName = string.Empty;
            SelectedSportCount = items.Count;

            if (SelectedSportCount == 1)
            {
                SelectedSportName = items.FirstOrDefault().Value;
            }

            CallbackModel.Sports = Sports.Where(c => c.Value != "all" && c.IsChecked).Select(s => new ManualBetSettlementCallbackModel.SettlementSport
            {
                SportId = long.Parse(s.Value),
                SportName = s.Name
            }).ToList();

            await OnInitCallback.InvokeAsync(CallbackModel);
        }

        public async Task OnCurrencyCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedCurrencyName = string.Empty;
            SelectedCurrencyCount = items.Count;

            if (SelectedCurrencyCount == 1)
            {
                SelectedCurrencyName = items.FirstOrDefault().Value;
            }

            CallbackModel.Currencies = Currencies.Where(c => c.Value != "all" && c.IsChecked).Select(s => new ManualBetSettlementCallbackModel.SettlementCurrency
            {
                CurrencyId = int.Parse(s.Value),
                CurrencyCode = s.Name
            }).ToList();

            await OnInitCallback.InvokeAsync(CallbackModel);
        }

        public async Task OnFromStatusCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedFromStatusName = string.Empty;
            SelectedFromStatusCount = items.Count;

            if (SelectedFromStatusCount == 1)
            {
                SelectedFromStatusName = items.FirstOrDefault().Value;
            }

            CallbackModel.FromBetStatuses = FromStatuses.Where(c => c.Value != "all" && c.IsChecked).Select(s => new ManualBetSettlementCallbackModel.SettlementBetStatus
            {
                BetStatusId = int.Parse(s.Value),
                BetStatusName = s.Name
            }).ToList();

            await OnInitCallback.InvokeAsync(CallbackModel);
        }

        public async Task OnToStatusCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedToStatusName = string.Empty;
            SelectedToStatusCount = items.Count;

            if (SelectedToStatusCount == 1)
            {
                SelectedToStatusName = items.FirstOrDefault().Value;
            }

            CallbackModel.ToBetStatuses = ToStatuses.Where(c => c.Value != "all" && c.IsChecked).Select(s => new ManualBetSettlementCallbackModel.SettlementBetStatus
            {
                BetStatusId = int.Parse(s.Value),
                BetStatusName = s.Name
            }).ToList();

            await OnInitCallback.InvokeAsync(CallbackModel);
        }

        public async Task OnReferenceIdChangedEvent(ChangeEventArgs e)
        {
            CallbackModel.BetId = e.Value.ToString();

            await OnInitCallback.InvokeAsync(CallbackModel);
        }

        public async Task OnDateRangeChangedEvent(DateRange range)
        {
            CallbackModel.DateFrom = range.Start.DateTime;
            CallbackModel.DateTo = range.End.DateTime;

            await OnInitCallback.InvokeAsync(CallbackModel);
        }

        public async Task OnMemberNameChangedEvent(ChangeEventArgs e)
        {
            CallbackModel.MemberName = e.Value.ToString();

            await OnInitCallback.InvokeAsync(CallbackModel);
        }

        public async Task OnSettledByChangedEvent(ChangeEventArgs e)
        {
            CallbackModel.SettledBy = e.Value.ToString();

            await OnInitCallback.InvokeAsync(CallbackModel);
        }

        public async Task OnSearchButtonClickedEvent()
        {
            await OnSearchButtonCallback.InvokeAsync();
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private async Task LoadClients()
        {
            var clients = (await BackofficeService.GetClients()).Result;

            if (clients != null && clients.Clients.Any())
            {
                if(_userState.ClientCodes.Any())
                    clients.Clients = clients.Clients.Where(c => _userState.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();

                Clients = clients.Clients.Select(s => new SelectListItemWithCheckboxModel<string>
                {
                    IsChecked = true,
                    Value = s.ClientId.ToString(),
                    Name = s.ClientName
                }).ToList();

                Clients.Insert(0, new SelectListItemWithCheckboxModel<string>
                {
                    IsChecked = true,
                    Value = "all",
                    Name = "All Clients"
                });

                var selectedClients = Clients.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();
                SelectedClientCount = selectedClients.Count;
                SelectedClientName = SelectedClientCount > 0 ? string.Empty : Clients.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;

                CallbackModel.Clients = Clients.Where(c => c.Value != "all" && c.IsChecked).Select(s => new ManualBetSettlementCallbackModel.SettlementClient
                {
                    ClientId = long.Parse(s.Value),
                    ClientName = s.Name
                }).ToList();
            }
        }

        private async Task LoadSports()
        {
            var sports = (await BackofficeService.GetSportsAsync()).Result;

            Sports = sports.SportLookup.Select(s => new SelectListItemWithCheckboxModel<string>
            {
                IsChecked = true,
                Value = s.SportId.ToString(),
                Name = s.SportName
            }).ToList();

            //temporarily add virtual sports since it is not handled in API
            List<SelectListItemWithCheckboxModel<string>> virtualSports = new List<SelectListItemWithCheckboxModel<string>>()
            {
                new SelectListItemWithCheckboxModel<string>
                {
                    IsChecked = true,
                    Value = 1001.ToString(),
                    Name = "Virtual Soccer"
                },
                new SelectListItemWithCheckboxModel<string>
                {
                    IsChecked = true,
                    Value = 1002.ToString(),
                    Name = "Virtual Basketball"
                },
                new SelectListItemWithCheckboxModel<string>
                {
                    IsChecked = true,
                    Value = 1003.ToString(),
                    Name = "Virtual Baseball"
                },
                new SelectListItemWithCheckboxModel<string>
                {
                    IsChecked = true,
                    Value = 1005.ToString(),
                    Name = "Virtual Tennis"
                },
                new SelectListItemWithCheckboxModel<string>
                {
                    IsChecked = true,
                    Value = 1021.ToString(),
                    Name = "Virtual Cricket"
                },
                new SelectListItemWithCheckboxModel<string>
                {
                    IsChecked = true,
                    Value = 1055.ToString(),
                    Name = "Virtual Horse Racing"
                },
                new SelectListItemWithCheckboxModel<string>
                {
                    IsChecked = true,
                    Value = 1058.ToString(),
                    Name = "Virtual Dog Racing"
                }
            };

            Sports.AddRange(virtualSports);

            Sports.Insert(0, new SelectListItemWithCheckboxModel<string>
            {
                IsChecked = true,
                Value = "all",
                Name = "All Sports"
            });

            var selectedSports = Sports.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();
            SelectedSportCount = selectedSports.Count;
            SelectedSportName = SelectedSportCount > 0 ? string.Empty : Sports.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;

            CallbackModel.Sports = Sports.Where(c => c.Value != "all" && c.IsChecked).Select(s => new ManualBetSettlementCallbackModel.SettlementSport
            {
                SportId = long.Parse(s.Value),
                SportName = s.Name
            }).ToList();
        }

        private async Task LoadCurrencies()
        {
            Currencies = new List<SelectListItemWithCheckboxModel<string>>();
            foreach (Currency currency in (Currency[])Enum.GetValues(typeof(Currency)))
            {
                if (currency == 0)
                {
                    Currencies.Add(new SelectListItemWithCheckboxModel<string>()
                    {
                        IsChecked = true,
                        Value = "all",
                        Name = "All Currencies"
                    });
                }
                else
                {
                    Currencies.Add(new SelectListItemWithCheckboxModel<string>()
                    {
                        IsChecked = true,
                        Value = ((int)currency).ToString(),
                        Name = currency.ToString()
                    });
                }
            }

            var selectedCurrencies = Currencies.Where(c => c.Value != "All" && c.IsChecked).Select(s => s.Value).ToList();
            SelectedCurrencyCount = selectedCurrencies.Count;
            SelectedCurrencyName = SelectedCurrencyCount > 0 ? string.Empty : Currencies.FirstOrDefault(c => c.Value != "All" && c.IsChecked).Name;

            CallbackModel.Currencies = Currencies.Where(c => c.Value != "all" && c.IsChecked).Select(s => new ManualBetSettlementCallbackModel.SettlementCurrency
            {
                CurrencyId = int.Parse(s.Value),
                CurrencyCode = s.Name
            }).ToList();
        }

        private async Task LoadFromStatus()
        {
            //running, won, lost, halfwon,halflost, void
            FromStatuses = new List<SelectListItemWithCheckboxModel<string>>();
            var betStatusList = (BetStatus[])Enum.GetValues(typeof(BetStatus));

            foreach (BetStatus betStatus in betStatusList)
            {
                int betStatusCasted = (int)betStatus;

                if (betStatusCasted == 1000 || betStatusCasted == 6000 || betStatusCasted == 7000)
                {
                    continue;
                }
                else
                {
                    FromStatuses.Add(new SelectListItemWithCheckboxModel<string>()
                    {
                        IsChecked = true,
                        Value = ((int)betStatus).ToString(),
                        Name = betStatus.ToString()
                    });
                }
            }

            FromStatuses.Insert(0, new SelectListItemWithCheckboxModel<string>
            {
                IsChecked = true,
                Value = "all",
                Name = "All From Status"
            });

            var selectedFromStatuses = FromStatuses.Where(c => c.Value != "All" && c.IsChecked).Select(s => s.Value).ToList();
            SelectedFromStatusCount = selectedFromStatuses.Count;
            SelectedFromStatusName = SelectedFromStatusCount > 0 ? string.Empty : FromStatuses.FirstOrDefault(c => c.Value != "All" && c.IsChecked).Name;

            CallbackModel.FromBetStatuses = FromStatuses.Where(c => c.Value != "all" && c.IsChecked).Select(s => new ManualBetSettlementCallbackModel.SettlementBetStatus
            {
                BetStatusId = int.Parse(s.Value),
                BetStatusName = s.Name
            }).ToList();
        }

        private async Task LoadToStatus()
        {
            ToStatuses = new List<SelectListItemWithCheckboxModel<string>>();
            var betStatusList = (BetStatus[])Enum.GetValues(typeof(BetStatus));

            foreach (BetStatus betStatus in betStatusList)
            {
                int betStatusCasted = (int)betStatus;

                if (betStatusCasted == 1000 || betStatusCasted == 6000 || betStatusCasted == 7000)
                {
                    continue;
                }
                else
                {
                    ToStatuses.Add(new SelectListItemWithCheckboxModel<string>()
                    {
                        IsChecked = true,
                        Value = ((int)betStatus).ToString(),
                        Name = betStatus.ToString()
                    });
                }
            }

            ToStatuses.Insert(0, new SelectListItemWithCheckboxModel<string>
            {
                IsChecked = true,
                Value = "all",
                Name = "All From Status"
            });

            var selectedToStatuses = ToStatuses.Where(c => c.Value != "All" && c.IsChecked).Select(s => s.Value).ToList();
            SelectedToStatusCount = selectedToStatuses.Count;
            SelectedToStatusName = SelectedToStatusCount > 0 ? string.Empty : ToStatuses.FirstOrDefault(c => c.Value != "All" && c.IsChecked).Name;

            CallbackModel.ToBetStatuses = ToStatuses.Where(c => c.Value != "all" && c.IsChecked).Select(s => new ManualBetSettlementCallbackModel.SettlementBetStatus
            {
                BetStatusId = int.Parse(s.Value),
                BetStatusName = s.Name
            }).ToList();
        }
        #endregion
    }
}
