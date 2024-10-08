using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Backoffice.Web.Shared.OddsEngine
{
    public partial class HeaderNavMenu : IDisposable
    {
        #region Properties

        [Inject] public NavigationManager? NavigationManager { get; set; }
        [Parameter] public string Region { get; set; } = "hk";

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        private bool _showPriceBoostMenu;
        private bool _showMarketSettingsMenu;
        private bool _isActiveMonitoring;
        private bool _isActiveConfig;
        private bool _isActivePriceBoost;
        private bool _isActiveSportSettings;
        private bool _isActiveMarketSettings;
        private string _address = string.Empty;
        private CancellationTokenSource? _cts;
        #endregion

        private OeNavMenu _menu;
        private string? _activePage;

        #region Methods

        protected override async Task OnInitializedAsync() => NavigationManager.LocationChanged += HandleLocationChanged;
        protected override void OnParametersSet()
        {
            _address = $"{Region.ToLower()}/odds-engine";
            var page = NavigationManager?.Uri.Replace(NavigationManager.BaseUri, "");

            if (_activePage != page)
            {
                _activePage = page;
                ActivateMenu(page);
            }
        }
        private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            var page = NavigationManager?.Uri.Replace(NavigationManager.BaseUri, "");
            ActivateMenu(page);
        }
        public void ActivateMenu(string? menu = null)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            var token = _cts.Token;
            var tempMenu = _menu;
            HideAllMenu();

            if (!string.IsNullOrEmpty(menu))
            {
                if (menu.Contains("hk/odds-engine/"))
                    menu = menu.Replace("hk/odds-engine/", "");
                if (menu.Contains("hk/odds-engine"))
                    menu = menu.Replace("hk/odds-engine", "");

                tempMenu = menu.ToUpper() switch
                {
                    "CONFIG" or "CONFIG-TEMPLATES" => OeNavMenu.Config,
                    "PRICEBOOST" or "MANAGE-BOOST" or "BOOSTED-SETTINGS" => OeNavMenu.PriceBoost,
                    "SPORT-SETTINGS" => OeNavMenu.SportSettings,
                    "MARKET-SETTINGS" or "MARKET-SORT-ORDER" or "MARKET-GROUPS" => OeNavMenu.MarketSettings,
                    _ or "MONITORING" => OeNavMenu.Monitoring
                };

                if (token.IsCancellationRequested)
                    return;

                _menu = tempMenu;
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
        private void OnMarketSettingsClickEvent()
        {
            if (!_showMarketSettingsMenu)
            {
                HideAllMenu();

                _showMarketSettingsMenu = true;
            }
            else
                _showMarketSettingsMenu = false;
        }
        public void Dispose() => NavigationManager.LocationChanged -= HandleLocationChanged;

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
