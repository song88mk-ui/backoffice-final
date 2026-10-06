using Backoffice.Shared.Enums;
using Backoffice.Web.Models.OddsEngine.Monitoring;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Monitoring
{
    public partial class MonitoringSelectionRow : IDisposable
    {
        #region Properties
        [Parameter] public MonitoringFixtureSelectionModel Selection { get; set; }
        [Parameter] public bool IsBetStop { get; set; }
        [Parameter] public int CurrentMarketStatus { get; set; }

        public Dictionary<string, object> SpanAttributes { get; set; } = new Dictionary<string, object>();

        #endregion

        #region Fields

        private string _color = "odds-bg-default";

        private bool _init = true;
        private bool _shouldRender;
        private bool _isBetStop;
        private int _currentMarketStatus;
        private string _odds;
        private long _unixTime;
        private bool _fading = true;

        private CancellationTokenSource _cts;

        #endregion

        #region Methods

        protected override async Task OnParametersSetAsync()
        {
            var color = GetColor();

            _shouldRender = ValidateParameters() || _color != color;

            if (_shouldRender)
            {
                _unixTime = Selection.UnixTime;
                _isBetStop = IsBetStop;
                _currentMarketStatus = CurrentMarketStatus;

                if (_color != color)
                {
                    _color = color;

                    _cts?.Cancel();
                    if (_fading)
                    {
                        _cts?.Dispose();
                        _cts = new CancellationTokenSource();
                        await Task.Delay(4000, _cts.Token);

                        _color = "odds-bg-default";
                        _shouldRender = true;
                    }
                }
            }

            _init = false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender) => _shouldRender = false;

        protected override bool ShouldRender() => _shouldRender;


        public void Dispose()
        {
            _cts?.Dispose();
        }

        #endregion

        #region Utilities

        private bool ValidateParameters() => _init
                || _isBetStop != IsBetStop
                || _currentMarketStatus != CurrentMarketStatus
                || _unixTime != Selection.UnixTime;

        private string GetColor()
        {
            _fading = true;
            var color = "odds-bg-default";

            SpanAttributes["data-title"] = string.Empty;

            if (IsBetStop || CurrentMarketStatus != (int)MarketStatus.Active || !Selection.IsActive)
            {
                _fading = false;
                if (CurrentMarketStatus == (int)MarketStatus.Inactive || !Selection.IsActive)
                {
                    SpanAttributes["data-title"] = MarketStatus.Inactive.ToString();
                    color = "odds-bg-inactive";
                }
                else
                {
                    SpanAttributes["data-title"] = "Bet Stop";
                    color = "odds-bg-betstop";
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(_odds) && Selection.DecimalOdds != "-.-")
                {
                    if (Convert.ToDecimal(_odds) > Convert.ToDecimal(Selection.DecimalOdds))
                    {
                        color = "odds-bg-danger-active";
                    }

                    if (Convert.ToDecimal(_odds) < Convert.ToDecimal(Selection.DecimalOdds))
                    {
                        color = "odds-bg-success-active";
                    }
                }

            }

            _odds = Selection.DecimalOdds;
            return color;
        }


        #endregion
    }
}
