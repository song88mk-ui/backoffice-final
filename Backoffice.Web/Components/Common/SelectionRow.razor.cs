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

        public Dictionary<string, object> SpanAttributes { get; set; }

        #endregion

        #region Fields

        private string _color = "";

        private bool _init = true;
        private bool _shouldRender;
        private bool _isBetStop;
        private int _currentMarketStatus;
        private string _correlationId;

        private CancellationTokenSource _cts;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SpanAttributes = new Dictionary<string, object>();
        }

        protected override async Task OnParametersSetAsync()
        {
            var color = GetColor();

            _shouldRender = ValidateParameters() || _color != color;

            if (_shouldRender)
            {
                _isBetStop = IsBetStop;
                _currentMarketStatus = CurrentMarketStatus;

                if (_color != color)
                {
                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();

                    if (!string.IsNullOrEmpty(_color) && !_color.Contains("inactive") && !_color.Contains("betstop"))
                    {
                        _color = color;
                        await Task.Delay(4000, _cts.Token);

                        _color = "odds-bg-default";
                        Selection.OldDecimalOdds = string.Empty;
                        Selection.ClientOdds.ForEach(c => { c.OldOdds = 0; });
                    }
                    else
                        _color = color;
                }
            }

            _init = false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            _shouldRender = false;
        }

        protected override bool ShouldRender() => _shouldRender;


        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private bool ValidateParameters() => _init
                || _isBetStop != IsBetStop
                || _currentMarketStatus != CurrentMarketStatus
                || _correlationId != Selection.CorrelationId;

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
                    if (!string.IsNullOrEmpty(Selection.OldDecimalOdds))
                    {
                        if (Convert.ToDecimal(Selection.OldDecimalOdds) > Convert.ToDecimal(Selection.DecimalOdds))
                        {
                            color = "odds-bg-danger-active";
                        }

                        if (Convert.ToDecimal(Selection.OldDecimalOdds) < Convert.ToDecimal(Selection.DecimalOdds))
                        {
                            color = "odds-bg-success-active";
                        }
                    }
                }
                else
                {
                    if (Selection.ClientOdds != null)
                    {
                        var clientOdds = Selection.ClientOdds.Where(c => c.ClientCode == CurrentClientCode && c.SelectionTemplateId == Selection.SelectionTemplateId).FirstOrDefault();
                        if (clientOdds != null)
                        {
                            if (clientOdds.OldOdds != 0)
                            {
                                if (clientOdds.OldOdds > clientOdds.Odds)
                                {
                                    color = "odds-bg-danger-active";
                                }

                                if (clientOdds.OldOdds < clientOdds.Odds)
                                {
                                    color = "odds-bg-success-active";
                                }
                            }
                        }
                    }
                }
            }

            return color;
        }


        #endregion
    }
}
