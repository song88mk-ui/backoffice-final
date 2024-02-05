using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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

        private OeNavMenu _menu;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            NavigationManager.LocationChanged += HandleLocationChanged;
        }
        protected override async Task OnParametersSetAsync()
        {
            _address = $"{Region.ToLower()}/odds-engine";
        }
        private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            var page = NavigationManager?.Uri.Replace(NavigationManager.BaseUri, "");
            ActivateMenu(page);
        }
        public void ActivateMenu(string? menu = null)
        {
            HideAllMenu();

            if (menu != null)
            {
                if (menu.Contains("hk/odds-engine/"))
                    menu = menu.Replace("hk/odds-engine/", "");
                //if (menu.Contains("hk/odds-engine"))
                //    menu = menu.Replace("hk/odds-engine", "");

                switch (menu.ToUpper())
                {
                    case "":
                    case "MONITORING":
                        _menu = OeNavMenu.Monitoring;
                        break;
                    case "CONFIG":
                    case "CONFIG-TEMPLATES":
                        _menu = OeNavMenu.Config;
                        break;
                    case "PRICEBOOST":
                    case "MAMAGE-BOOST":
                    case "BOOSTED-SETTINGS":
                        _menu = OeNavMenu.PriceBoost;
                        break;
                    case "SPORT-SETTINGS":
                        _menu = OeNavMenu.SportSettings;
                        break;
                    case "MARKET-SETTINGS":
                    case "MARKET-SORT-ORDER":
                    case "MARKET-GROUPS":
                        _menu = OeNavMenu.MarketSettings;
                        break;
                }
                StateHasChanged();
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
            NavigationManager.LocationChanged -= HandleLocationChanged;
        }

        #endregion

        #region Utilities

        private void HideAllMenu()
        {
            _showPriceBoostMenu = false;
            _showMarketSettingsMenu = false;
        }
        #endregion
    }
}
