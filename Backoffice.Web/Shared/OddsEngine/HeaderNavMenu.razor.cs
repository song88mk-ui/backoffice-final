namespace Backoffice.Web.Shared.OddsEngine
{
    public partial class HeaderNavMenu : IDisposable
    {
        #region Properties

        public bool ShowPriceBoostMenu { get; set; }
        public bool ShowAdministrationMenu { get; set; }
        public bool ShowSettlementMenu { get; set; }
        public bool ShowMarketSettingsMenu { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            HideAllMenu();
            HideAllSubmenu();
        }

        public async Task OnPriceBoostClickEvent()
        {
            if (!ShowPriceBoostMenu)
            {
                HideAllMenu();
                HideAllSubmenu();

                ShowPriceBoostMenu = true;
            }
            else
                ShowPriceBoostMenu = false;
        }

        public async Task OnAdministrationClickEvent()
        {
            if (!ShowAdministrationMenu)
            {
                HideAllMenu();
                HideAllSubmenu();

                ShowAdministrationMenu = true;
            }
            else
                ShowAdministrationMenu = false;
        }

        public async Task OnSettlementClickEvent()
        {
            if (!ShowSettlementMenu)
            {
                HideAllSubmenu();

                ShowSettlementMenu = true;
            }
            else
                ShowSettlementMenu = false;
        }

        public async Task OnMarketSettingsClickEvent()
        {
            if (!ShowMarketSettingsMenu)
            {
                HideAllSubmenu();

                ShowMarketSettingsMenu = true;
            }
            else
                ShowMarketSettingsMenu = false;
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private void HideAllMenu()
        {
            ShowPriceBoostMenu = false;
            ShowAdministrationMenu = false;
            ShowSettlementMenu = false;
        }

        private void HideAllSubmenu()
        {
            ShowSettlementMenu = false;
            ShowMarketSettingsMenu = false;
        }

        #endregion
    }
}
