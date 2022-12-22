namespace Backoffice.Web.Components.Clients
{
    public partial class CreateModal : IDisposable
    {
        #region Properties
        public bool companyRevenueToggle { get; set; }
        public List<string> Currenycies { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            companyRevenueToggle = false;

            Currenycies = new List<string>() 
            {
                "USD", "EUR", "GBP", "KRW", "VND", "THB", "CNY", "MYR", "KHR", "IDR", "INR", "JPY",
                "MMK", "HKD", "AED", "TRY", "TWD", "NZD", "ZAR", "KES", "TZS", "GHS", "ZMW", "MZN",
                "CDF", "ETB", "MWK", "XAF", "NGN", "BTC", "ETH", "PKR", "BRL", "CLP", "PEN", "MXN"
            };
        }
        public void Dispose()
        {
            
        }

        public async Task ToggleCompanyRevenueSwitch()
        {
            //false = fixed
            //true = tier
            companyRevenueToggle= !companyRevenueToggle;

            await InvokeAsync(StateHasChanged);
        }
        #endregion
    }
}
