using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Diagnostics.CodeAnalysis;

namespace Backoffice.Web.Components.ManualBetSettlement
{
    public partial class Filter : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [Parameter] public ManualBetSettlementFilterModel FilterModel { get; set; }
        [Parameter] public EventCallback<ManualBetSettlementFilterModel> FilterModelChanged { get; set; }
        [Parameter] public EventCallback OnSearchButtonCallback { get; set; }

        public bool ShowFilter { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ShowFilter = false;

            await LoadClients();
            await LoadSports();
            await LoadCurrencies();
            await LoadFromStatus();
            await LoadToStatus();
        }

        public async Task OnAddReferenceClickEvent()
        {
            ModalParameters parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 80rem");

            ModalOptions options = new ModalOptions();

            ModalService.Open<AddReferenceModal>("Add Reference ID", parameters, options);
        }

        public async Task OnFilterClickEvent()
        {
            ShowFilter = !ShowFilter;
        }

        public async Task OnClientCheckboxClickEvent(Dictionary<string, string> items)
        {
            FilterModel.SelectedClientName = string.Empty;
            FilterModel.SelectedClientCount = items.Count;

            if (FilterModel.SelectedClientCount == 1)
            {
                FilterModel.SelectedClientName = items.FirstOrDefault().Value;
            }
        }

        public async Task OnSportCheckboxClickEvent(Dictionary<string, string> items)
        {
            FilterModel.SelectedSportName = string.Empty;
            FilterModel.SelectedSportCount = items.Count;

            if (FilterModel.SelectedSportCount == 1)
            {
                FilterModel.SelectedSportName = items.FirstOrDefault().Value;
            }
        }

        public async Task OnCurrencyCheckboxClickEvent(Dictionary<string, string> items)
        {
            FilterModel.SelectedCurrencyName = string.Empty;
            FilterModel.SelectedCurrencyCount = items.Count;

            if (FilterModel.SelectedCurrencyCount == 1)
            {
                FilterModel.SelectedCurrencyName = items.FirstOrDefault().Value;
            }
        }

        public async Task OnFromStatusCheckboxClickEvent(Dictionary<string, string> items)
        {
            FilterModel.FromSelectedStatusName = string.Empty;
            FilterModel.FromSelectedStatusCount = items.Count;

            if (FilterModel.FromSelectedStatusCount == 1)
            {
                FilterModel.FromSelectedStatusName = items.FirstOrDefault().Value;
            }
        }

        public async Task OnToStatusCheckboxClickEvent(Dictionary<string, string> items)
        {
            FilterModel.ToSelectedStatusName = string.Empty;
            FilterModel.ToSelectedStatusCount = items.Count;

            if (FilterModel.ToSelectedStatusCount == 1)
            {
                FilterModel.ToSelectedStatusName = items.FirstOrDefault().Value;
            }
        }

        public async Task OnDateRangeChangedEvent(DateRange range)
        {
            FilterModel.DateFrom = DateTime.Parse(range.Start.ToString()).Date.Add(new TimeSpan(0, 0, 0));
            FilterModel.DateTo = DateTime.Parse(range.End.ToString()).Date.Add(new TimeSpan(23, 59, 59));
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
            var clients = (await BackOfficeService.GetClients()).Result;

            if (clients != null && clients.Clients.Any())
            {
                if (UserModel.ClientCodes.Any())
                    clients.Clients = clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();

                FilterModel.Clients = clients.Clients.Select(s => new SelectListItemWithCheckboxModel<string>
                {
                    IsChecked = true,
                    Value = s.ClientId.ToString(),
                    Name = s.ClientName
                }).ToList();

                FilterModel.Clients.Insert(0, new SelectListItemWithCheckboxModel<string>
                {
                    IsChecked = true,
                    Value = "all",
                    Name = "All Clients"
                });

                var selectedClients = FilterModel.Clients.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

                FilterModel.SelectedClientCount = selectedClients.Count;
                FilterModel.SelectedClientName = FilterModel.SelectedClientCount > 0 ? string.Empty : FilterModel.Clients.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
            }
        }

        private async Task LoadSports()
        {
            var sports = (await BackOfficeService.GetSportsAsync()).Result;

            FilterModel.Sports = sports.SportLookup.Select(s => new SelectListItemWithCheckboxModel<string>
            {
                IsChecked = true,
                Value = s.SportId.ToString(),
                Name = s.SportName
            }).ToList();

            FilterModel.Sports.Insert(0, new SelectListItemWithCheckboxModel<string>
            {
                IsChecked = true,
                Value = "all",
                Name = "All Sports"
            });

            var selectedSports = FilterModel.Sports.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

            FilterModel.SelectedSportCount = selectedSports.Count;
            FilterModel.SelectedSportName = FilterModel.SelectedSportCount > 0 ? string.Empty : FilterModel.Sports.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
        }

        private async Task LoadCurrencies()
        {
            FilterModel.Currencies = new List<SelectListItemWithCheckboxModel<string>>();
            foreach (Currency currency in (Currency[])Enum.GetValues(typeof(Currency)))
            {
                if (currency == 0)
                {
                    FilterModel.Currencies.Add(new SelectListItemWithCheckboxModel<string>()
                    {                        
                        IsChecked = true,
                        Value = ((int)currency).ToString(),
                        Name = "All Currencies"
                    });
                }
                else
                {
                    FilterModel.Currencies.Add(new SelectListItemWithCheckboxModel<string>()
                    {
                        IsChecked = true,
                        Value = ((int)currency).ToString(),
                        Name = currency.ToString()
                    });
                }
            }

            var selectedCurrencies = FilterModel.Currencies.Where(c => c.Value != "All" && c.IsChecked).Select(s => s.Value).ToList();
            FilterModel.SelectedCurrencyCount = selectedCurrencies.Count;
            FilterModel.SelectedCurrencyName = FilterModel.SelectedCurrencyCount > 0 ? string.Empty : FilterModel.Currencies.FirstOrDefault(c => c.Value != "All" && c.IsChecked).Name;
        }

        private async Task LoadFromStatus()
        {
            FilterModel.FromStatuses = new List<SelectListItemWithCheckboxModel<string>>();
            foreach (BetStatus betStatus in (BetStatus[])Enum.GetValues(typeof(BetStatus)))
            {
                if ((int)betStatus != 1000 || (int)betStatus != 6000 || (int)betStatus != 7000)
                {
                    FilterModel.FromStatuses.Add(new SelectListItemWithCheckboxModel<string>()
                    {
                        IsChecked = true,
                        Value = ((int)betStatus).ToString(),
                        Name = betStatus.ToString()
                    });
                }
            }

            FilterModel.FromStatuses.Insert(0, new SelectListItemWithCheckboxModel<string>
            {
                IsChecked = true,
                Value = "all",
                Name = "All From Status"
            });

            var selectedFromStatuses = FilterModel.FromStatuses.Where(c => c.Value != "All" && c.IsChecked).Select(s => s.Value).ToList();
            FilterModel.FromSelectedStatusCount = selectedFromStatuses.Count;
            FilterModel.FromSelectedStatusName = FilterModel.FromSelectedStatusCount > 0 ? string.Empty : FilterModel.FromStatuses.FirstOrDefault(c => c.Value != "All" && c.IsChecked).Name;
        }

        private async Task LoadToStatus()
        {
            FilterModel.ToStatuses = new List<SelectListItemWithCheckboxModel<string>>();
            foreach (BetStatus betStatus in (BetStatus[])Enum.GetValues(typeof(BetStatus)))
            {
                if ((int)betStatus != 1000 || (int)betStatus != 6000 || (int)betStatus != 7000)
                {
                    FilterModel.ToStatuses.Add(new SelectListItemWithCheckboxModel<string>()
                    {
                        IsChecked = true,
                        Value = ((int)betStatus).ToString(),
                        Name = betStatus.ToString()
                    });
                }
            }

            FilterModel.ToStatuses.Insert(0, new SelectListItemWithCheckboxModel<string>
            {
                IsChecked = true,
                Value = "all",
                Name = "All From Status"
            });

            var selectedToStatuses = FilterModel.ToStatuses.Where(c => c.Value != "All" && c.IsChecked).Select(s => s.Value).ToList();
            FilterModel.ToSelectedStatusCount = selectedToStatuses.Count;
            FilterModel.ToSelectedStatusName = FilterModel.ToSelectedStatusCount > 0 ? string.Empty : FilterModel.ToStatuses.FirstOrDefault(c => c.Value != "All" && c.IsChecked).Name;
        }
        #endregion
    }
}
