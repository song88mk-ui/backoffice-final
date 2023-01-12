using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Net;

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
        public bool companyRevenueToggle { get; set; }
        public bool monthlyFeeToggle { get; set; }
        public List<string> Currencies { get; set; }
        public List<int> SelectedRealSports { get; set; }
        public List<int> SelectedVirtualSports { get; set; }
        public bool IsEdit { get; set; }
        public List<AlertModel> Alerts { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            companyRevenueToggle = false;
            monthlyFeeToggle = false;

            SelectedRealSports = new List<int>();
            SelectedVirtualSports = new List<int>();

            Alerts = new List<AlertModel>();
            Client = ModalParameters.TryGet<ClientListResponse.Client>("Client");
            RealSports = ModalParameters.TryGet<SportsLookupModel>("RealSports");
            VirtualSports = ModalParameters.TryGet<SportsLookupModel>("VirtualSports");
            IsEdit = ModalParameters.TryGet<bool>("IsEdit");
            Client.StatusId = IsEdit ? Client.StatusId : (int)ClientStatus.Normal;
            Client.StatusName = IsEdit ? Client.StatusName : ClientStatus.Normal.ToString();

            Currencies = new List<string>()
            {
                "USD", "EUR", "GBP", "KRW", "VND", "THB", "CNY", "MYR", "KHR", "IDR", "INR", "JPY",
                "MMK", "HKD", "AED", "TRY", "TWD", "NZD", "ZAR", "KES", "TZS", "GHS", "ZMW", "MZN",
                "CDF", "ETB", "MWK", "XAF", "NGN", "BTC", "ETH", "PKR", "BRL", "CLP", "PEN", "MXN"
            };
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

        public async Task OnRealSportsClicked(ChangeEventArgs e, int sportId)
        {
            if (Convert.ToBoolean(e.Value) == true)
            {
                if(sportId == 0)
                {
                    SelectedRealSports.Clear();
                    SelectedRealSports.AddRange(RealSports.Sports.Select(s => s.SportId).ToList());
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
        public async Task OnVirtualSportsClicked(ChangeEventArgs e, int sportId)
        {
            if (Convert.ToBoolean(e.Value) == true)
            {
                if (!SelectedVirtualSports.Any(s => s == sportId))
                    SelectedVirtualSports.Add(sportId);

            }
            else
            {
                SelectedRealSports.Remove(sportId);
            }
        }

        public void Dispose()
        {

        }

        public async Task ToggleCompanyRevenueSwitch()
        {
            //false = fixed
            //true = tier
            companyRevenueToggle = !companyRevenueToggle;

            await InvokeAsync(StateHasChanged);
        }
        #endregion
    }
}
