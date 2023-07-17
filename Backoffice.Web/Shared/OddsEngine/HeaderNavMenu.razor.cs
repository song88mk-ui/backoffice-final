using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;

namespace Backoffice.Web.Shared.OddsEngine
{
    public partial class HeaderNavMenu : IDisposable
    {
        #region Properties

        [Inject] public NavigationManager? NavigationManager { get; set; }
        [CascadingParameter(Name = "UserClaims")] public List<Claim>? UserClaims { get; set; }
        [Parameter] public string Region { get; set; } = "hk";

        private bool _showPriceBoostMenu;
        private bool _showMarketSettingsMenu;
        private bool _isActiveMonitoring;
        private bool _isActiveConfig;
        private bool _isActivePriceBoost;
        private bool _isActiveSportSettings;
        private bool _isActiveMarketSettings;
        private string _address = string.Empty;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            var page = NavigationManager?.Uri.Replace(NavigationManager.BaseUri, "");
            ActivateMenu(page);
        }
        protected override async Task OnParametersSetAsync()
        {
            _address = $"{Region.ToLower()}/odds-engine";
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
                        _isActiveMonitoring = true;
                        break;
                    case "CONFIG":
                        _isActiveConfig = true;
                        break;
                    case "PRICEBOOST":
                    case "MAMAGE-BOOST":
                    case "BOOSTED-SETTINGS":
                        _isActivePriceBoost = true;
                        break;
                    case "SPORT-SETTINGS":
                        _isActiveSportSettings = true;
                        break;
                    case "MARKET-SETTINGS":
                    case "MARKET-SORT-ORDER":
                    case "MARKET-GROUPS":
                        _isActiveMarketSettings = true;
                        break;
                }
            }
        }
        private async Task OnPriceBoostClickEvent()
        {
            if (!_showPriceBoostMenu)
            {
                HideAllMenu();
                _showPriceBoostMenu = true;
            }
            else
                _showPriceBoostMenu = false;
        }
        private async Task OnMarketSettingsClickEvent()
        {
            if (!_showMarketSettingsMenu)
            {
                HideAllMenu();

                _showMarketSettingsMenu = true;
            }
            else
                _showMarketSettingsMenu = false;
        }
        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private void HideAllMenu()
        {
            _showPriceBoostMenu = false;
            _showMarketSettingsMenu = false;
        }
        private void InActiveAllMenu()
        {
            _isActiveMonitoring = false;
            _isActiveConfig = false;
            _isActivePriceBoost = false;
            _isActiveSportSettings = false;
            _isActiveMarketSettings = false;
        }

        #endregion
    }
}
