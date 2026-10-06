using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;

namespace Backoffice.Web.Components.OddsEngine.MonitoringDetails
{
    public partial class MatchDetailSelectionRow : IDisposable
    {
        #region Properties

        [Parameter] public SelectionModel Selection { get; set; }
        [Parameter] public FixtureMarketModel Market { get; set; }
        [Parameter] public List<Client>? Clients { get; set; }

        #endregion

        private List<Client>? _clients;
        private string _clientCode;
        private string _clientCodeMargin;

        private bool _shouldRender = true;
        private bool _init = true;
        private long _unixTimeMarket;
        private long _unixTimeSelection;
        private string _clientReference;

        #region Methods
        protected override async Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameters();

            if (_shouldRender)
            {
                _clientReference = string.Join(',', Clients.Select(s => s.ClientCode).OrderBy(c => c).Select(s => s.Trim()).ToList());

                if (Clients != null && !(_clients?.Equals(Clients) ?? false))
                {
                    _clients = new List<Client>(Clients);
                }

                _unixTimeMarket = Market.UnixTime;
                _unixTimeSelection = Selection.UnixTime;
                _clientCode = Market.ClientCode;
                _clientCodeMargin = Market.ClientCodeMargin;
            }

            _init = false;
        }
        protected override async Task OnAfterRenderAsync(bool firstRender) => _shouldRender = false;
        protected override bool ShouldRender() => _shouldRender;

        public bool HasClientConfig(string clientCode, string marketClientCode, string marketClientMargin)
        {
            if (string.IsNullOrEmpty(marketClientCode))
                marketClientCode = string.Empty;
            if (string.IsNullOrEmpty(marketClientMargin))
                marketClientMargin = string.Empty;

            if (marketClientCode.Split("|").Contains(clientCode) || marketClientMargin.Contains(clientCode))
            {
                return true;
            }

            return false;
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private bool ValidateParameters()
        {
            var clientReference = string.Join(',', Clients.Select(s => s.ClientCode).OrderBy(c => c).Select(s => s.Trim()).ToList());

            return (_init || _unixTimeMarket != Market.UnixTime || _unixTimeSelection != Selection.UnixTime || _clientReference != clientReference) && Market != null;
        }
        #endregion
    }
}
