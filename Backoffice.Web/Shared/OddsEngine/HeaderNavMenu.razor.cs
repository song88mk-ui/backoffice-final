using Backoffice.Shared.Enums;
using Microsoft.AspNetCore.Components.Routing;
using System.Security.Claims;

namespace Backoffice.Web.Shared.OddsEngine
{
    public partial class HeaderNavMenu : IDisposable
    {
        #region Fields

        private OeNavMenu _menu;

        private string _activePage;
        private string _address = "hk/odds-engine";

        private bool _showPriceBoostMenu;
        private bool _showMarketSettingsMenu;
        private bool _init = true;
        private bool _shouldRender;
        private List<Claim> _userClaims = new();

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _navigationManager.LocationChanged += HandleLocationChanged;
            _translationState.PropertyChanged += async (s, e) =>
            {
                _shouldRender = true;
                await InvokeAsync(StateHasChanged);
            };
        }


        protected override async Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameters();

            if (_shouldRender)
            {
                _userClaims = new List<Claim>(_userState.Claims);

                var page = _navigationManager?.Uri.Replace(_navigationManager.BaseUri, "");

                _activePage = page;
                ActivateMenu(page);
            }

            if (_userState.Claims.Any())
                _init = false;
        }

        protected override bool ShouldRender() => _shouldRender;

        protected override async Task OnAfterRenderAsync(bool firstRender) => _shouldRender = false;

        public async Task HandlePriceBoostClick()
        {
            if (!_showPriceBoostMenu)
            {
                HideAllMenu();
                _showPriceBoostMenu = true;
            }
            else
                _showPriceBoostMenu = false;

            _shouldRender = true;
        }

        public async Task HandleMarketSettingsClick()
        {
            if (!_showMarketSettingsMenu)
            {
                HideAllMenu();

                _showMarketSettingsMenu = true;
            }
            else
                _showMarketSettingsMenu = false;

            _shouldRender = true;
        }

        public async void ActivateMenu(string? menu = null)
        {
            HideAllMenu();

            if (menu != null)
            {
                if (menu.Contains("hk/odds-engine/"))
                    menu = menu.Replace("hk/odds-engine/", "");
                if (menu.Contains("hk/odds-engine"))
                    menu = menu.Replace("hk/odds-engine", "");

                menu = menu.Split('/').FirstOrDefault() ?? string.Empty;

                switch (menu.ToUpper())
                {
                    case "CONFIG":
                    case "CONFIG-TEMPLATES":
                        _menu = OeNavMenu.Config;
                        break;
                    case "PRICEBOOST":
                    case "MANAGE-BOOST":
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
                    default:
                        _menu = OeNavMenu.Monitoring;
                        break;
                }
            }
        }

        public bool ValidateParameters()
        {
            var page = _navigationManager?.Uri.Replace(_navigationManager.BaseUri, "");

            return _init || _activePage != page || _userClaims.SequenceEqual(_userState.Claims);
        }

        public void Dispose()
        {
            //_navigationManager.LocationChanged -= HandleLocationChanged;
        }

        #endregion

        #region Utilities

        private void HideAllMenu()
        {
            _showPriceBoostMenu = false;
            _showMarketSettingsMenu = false;
        }

        private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            var page = _navigationManager?.Uri.Replace(_navigationManager.BaseUri, "");
            ActivateMenu(page);
            _shouldRender = true;
            StateHasChanged();
        }
        private void HandleLanguageCHanged()
        {
        }

        #endregion
    }
}
