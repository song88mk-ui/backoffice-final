using Backoffice.Shared.Enums;
using Microsoft.AspNetCore.Components;

using static Backoffice.Shared.Models.Response.MarketFixtureResponse;

namespace Backoffice.Web.Components.Common
{
    public partial class SelectionRow : IDisposable
    {
        #region Properties
        [Parameter] public SelectionModel Selection { get; set; }
        [Parameter] public string CurrentClientCode { get; set; }
        [Parameter] public bool HasClientConfig { get; set; }
        [Parameter] public string ColumnSize { get; set; }
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
                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();
                    _color = color;
                    await Task.Delay(4000, _cts.Token);

                    _color = "odds-bg-default";
                    _shouldRender = true;
                }
            }

            _init = false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender) => _shouldRender = false;

        protected override bool ShouldRender() => _shouldRender;


        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private bool ValidateParameters() => _init
                || _isBetStop != IsBetStop
                || _currentMarketStatus != CurrentMarketStatus
                || _unixTime != Selection.UnixTime;

        private string GetClientOdds()
        {
            if (Selection.ClientOdds != null && Selection.ClientOdds.Any(c => c.ClientCode == CurrentClientCode))
            {
                return Selection.ClientOdds.Where(c => c.ClientCode == CurrentClientCode && c.SelectionTemplateId == Selection.SelectionTemplateId).FirstOrDefault().Odds.ToString("0.00");
            }

            return Selection.DecimalOdds;
        }

        private string GetColor()
        {
            var color = "odds-bg-default";

            SpanAttributes["data-title"] = string.Empty;

            if (IsBetStop || CurrentMarketStatus != (int)MarketStatus.Active)
            {
                if (CurrentMarketStatus == (int)MarketStatus.Inactive)
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
                if (CurrentClientCode == null)
                {
                    if (!string.IsNullOrEmpty(_odds))
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

                    //_odds = Selection.DecimalOdds;
                }
                else
                {
                    if (Selection.ClientOdds != null)
                    {
                        var clientOdds = Selection.ClientOdds.Where(c => c.ClientCode == CurrentClientCode && c.SelectionTemplateId == Selection.SelectionTemplateId).FirstOrDefault();
                        if (clientOdds != null)
                        {
                            if(!string.IsNullOrEmpty(_odds))
                            {
                                if (Convert.ToDecimal(_odds) > clientOdds.Odds)
                                {
                                    color = "odds-bg-danger-active";
                                }

                                if (Convert.ToDecimal(_odds) < clientOdds.Odds)
                                {
                                    color = "odds-bg-success-active";
                                }
                            }
                        }

                        //_odds = clientOdds?.Odds.ToString("0.00") ?? "-.-";
                    }
                }
            }

            _odds = GetClientOdds();
            return color;
        }


        #endregion
    }
}
