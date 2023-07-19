using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using RabbitMQ.Client.Apigen.Attributes;
using System.ComponentModel;
using System.Diagnostics;

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

        private string _address = string.Empty;
        #endregion

        private OeNavMenu _menu;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            var page = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "");
            _address = $"{Region.ToLower()}/odds-engine";
            Console.WriteLine(" = = = = = Initialize");
            ActivateMenu(page);
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

        public async void ActivateMenu(string? menu = null)
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

        #endregion
    }
}
