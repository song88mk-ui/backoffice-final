using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Shared.OddsEngine
{
    public partial class HeaderNavMenu : IDisposable
    {
        #region Properties

        [Inject] public NavigationManager NavigationManager { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [Parameter] public string Region { get; set; } = "hk";

        public bool ShowPriceBoostMenu { get; set; }
        public bool ShowMarketSettingsMenu { get; set; }

        public bool IsActiveMonitoring { get; set; }
        public bool IsActiveConfig { get; set; }
        public bool IsActivePriceBoost { get; set; }
        public bool IsActiveSportSettings { get; set; }
        public bool IsActiveMarketSettings { get; set; }

        private string _address = string.Empty;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            var page = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "");
            _address = $"{Region.ToLower()}/odds-engine";
            ActivateMenu(page);

            HideAllMenu();

        }

        public async Task OnPriceBoostClickEvent()
        {
            if (!ShowPriceBoostMenu)
            {
                HideAllMenu();

                ShowPriceBoostMenu = true;
            }
            else
                ShowPriceBoostMenu = false;
        }

        public async Task OnMarketSettingsClickEvent()
        {
            if (!ShowMarketSettingsMenu)
            {
                HideAllMenu();

                ShowMarketSettingsMenu = true;
            }
            else
                ShowMarketSettingsMenu = false;
        }

        public void ActivateMenu(string? menu = null)
        {
            HideAllMenu();

            if (menu != null)
            {
                //if (menu.Contains("sa/odds-engine/"))
                //    menu = menu.Replace("sa/odds-engine/", "");
                //if (menu.Contains("sa/odds-engine"))
                //    menu = menu.Replace("sa/odds-engine", "");
                if (menu.Contains("hk/odds-engine/"))
                    menu = menu.Replace("hk/odds-engine/", "");
                if (menu.Contains("hk/odds-engine"))
                    menu = menu.Replace("hk/odds-engine", "");

                InActiveAllMenu();

                switch (menu.ToUpper())
                {
                    case "":
                    case "MONITORING":
                        IsActiveMonitoring = true;
                        break;
                    case "CONFIG":
                    case "CONFIG-TEMPLATES":
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
                }
            }
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private void HideAllMenu()
        {
            ShowPriceBoostMenu = false;
            ShowMarketSettingsMenu = false;
        }

        private void InActiveAllMenu()
        {
            IsActiveMonitoring = false;
            IsActiveConfig = false;
            IsActivePriceBoost = false;
            IsActiveSportSettings = false;
            IsActiveMarketSettings = false;
        }

        #endregion
    }
}
