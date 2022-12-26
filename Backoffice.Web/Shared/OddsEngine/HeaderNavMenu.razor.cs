namespace Backoffice.Web.Shared.OddsEngine
{
    public partial class HeaderNavMenu : IDisposable
    {
        #region Properties

        public bool ShowPriceBoostMenu { get; set; }
        public bool ShowAdministrationMenu { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ShowPriceBoostMenu = false;
            ShowAdministrationMenu = false;
        }

        public async Task OnPriceBoostClickEvent()
        {
            if (!ShowPriceBoostMenu) 
                ShowPriceBoostMenu = true;
            else if (ShowPriceBoostMenu) 
                ShowPriceBoostMenu = false;
        }

        public async Task OnAdministrationClickEvent()
        {
            if (!ShowAdministrationMenu)
                ShowAdministrationMenu = true;
            else if (ShowAdministrationMenu)
                ShowAdministrationMenu = false;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
