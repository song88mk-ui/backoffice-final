using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Shared.OddsEngine
{
    public partial class HeaderNavMenu : IDisposable
    {
        #region Properties
        [Inject] public NavigationManager NavigationManager { get; set; }

        public bool ShowPriceBoostMenu { get; set; }
        //public bool ShowAdministrationMenu { get; set; }
        //public bool ShowSettlementMenu { get; set; }
        public bool ShowMarketSettingsMenu { get; set; }

        public bool IsActiveMonitoring { get; set; }
        public bool IsActiveConfig { get; set; }
        public bool IsActivePriceBoost { get; set; }
        public bool IsActiveSportSettings { get; set; }
        public bool IsActiveMarketSettings { get; set; }
        //public bool IsActiveAdmin { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            var page = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "");
            CloseMenus(page);

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

        //public async Task OnAdministrationClickEvent()
        //{
        //    if (!ShowAdministrationMenu)
        //    {
        //        HideAllMenu();
        //        HideAllSubmenu();

        //        ShowAdministrationMenu = true;
        //    }
        //    else
        //        ShowAdministrationMenu = false;
        //}

        //public async Task OnSettlementClickEvent()
        //{
        //    if (!ShowSettlementMenu)
        //    {
        //        HideAllSubmenu();

        //        ShowSettlementMenu = true;
        //    }
        //    else
        //        ShowSettlementMenu = false;
        //}

        public async Task OnMarketSettingsClickEvent()
        {
            if (!ShowMarketSettingsMenu)
            {
                HideAllMenu();
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
            //ShowAdministrationMenu = false;
            //ShowSettlementMenu = false;
            ShowMarketSettingsMenu = false;
        }

        private void HideAllSubmenu()
        {
            //ShowSettlementMenu = false;
        }

        private void CloseMenus(string? menu = null)
        {
            HideAllMenu();

            HideAllSubmenu();
            if (menu != null)
                ActiveMenu(menu);
        }
        private void ActiveMenu(string menu)
        {
            if (menu.Contains("odds-engine/"))
                menu = menu.Replace("odds-engine/", "");
            if (menu.Contains("odds-engine"))
                menu = menu.Replace("odds-engine", "");

            InActiveAllMenu();

            switch (menu.ToUpper())
            {
                case "":
                case "MONITORING":
                    IsActiveMonitoring = true;
                    break;
                case "CONFIG":
                    IsActiveConfig = true;
                    break;
                case "PRICEBOOST":
                case "MAMAGE-BOOST":
                case "BOOSTED-SETTINGS":
                    IsActivePriceBoost = true;
                    break;
                case "SPORT-SETTINGS":
                    IsActiveSportSettings = true;
                    break;
                case "MARKET-SETTINGS":
                case "MARKET-SORT-ORDER":
                case "MARKET-GROUPS":
                    IsActiveMarketSettings = true;
                    break;
                //case "ADMIN":
                //case "MANAGE-USERGROUP":
                //case "MARKET-SETTLEMENT":
                //case "MESSAGE-SETTLEMENT":
                //case "MARKET-SORT-ORDER":
                //case "MARKET-GROUPS":
                //    IsActiveAdmin = true;
                //    break;
            }
        }
        private void InActiveAllMenu()
        {
            IsActiveMonitoring = false;
            IsActiveConfig = false;
            IsActivePriceBoost = false;
            IsActiveSportSettings = false;
            IsActiveMarketSettings = false;

            //IsActiveAdmin = false;
        }
        #endregion
    }
}
