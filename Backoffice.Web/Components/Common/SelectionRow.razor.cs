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

        public string Color { get; set; }
        public Dictionary<string, object> SpanAttributes { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SpanAttributes = new Dictionary<string, object>();

        }
        protected override async Task OnParametersSetAsync()
        {
            Color = "odds-bg-default";

            SpanAttributes["data-title"] = string.Empty;

            if (IsBetStop || CurrentMarketStatus != (int)MarketStatus.Active)
            {
                if (CurrentMarketStatus == (int)MarketStatus.Inactive)
                {
                    SpanAttributes["data-title"] = MarketStatus.Inactive.ToString();
                    Color = "odds-bg-inactive";
                }
                else
                {
                    SpanAttributes["data-title"] = "Bet Stop";
                    Color = "odds-bg-betstop";
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(Selection.OldDecimalOdds))
                {
                    if (Convert.ToDecimal(Selection.OldDecimalOdds) > Convert.ToDecimal(Selection.DecimalOdds))
                    {
                        Color = "odds-bg-danger-active";
                    }

                    if (Convert.ToDecimal(Selection.OldDecimalOdds) < Convert.ToDecimal(Selection.DecimalOdds))
                    {
                        Color = "odds-bg-success-active";
                    }
                }
            }
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities
        public void OnCompleted(string cssPropertyName)
        {
            Color = "odds-bg-default";
            Selection.OldDecimalOdds = string.Empty;
            Selection.ClientOdds.ForEach(c => { c.OldOdds = 0; });
        }

        public string GetClientOdds()
        {
            if (Selection.ClientOdds != null && Selection.ClientOdds.Any(c => c.ClientCode == CurrentClientCode))
            {
                return Selection.ClientOdds.Where(c => c.ClientCode == CurrentClientCode && c.SelectionTemplateId == Selection.SelectionTemplateId).FirstOrDefault().Odds.ToString("0.00");
            }

            return Selection.DecimalOdds;
        }

        public string GetClientColor()
        {
            Color = "odds-bg-default";

            SpanAttributes["data-title"] = string.Empty;

            if (IsBetStop || CurrentMarketStatus != (int)MarketStatus.Active)
            {
                if (CurrentMarketStatus == (int)MarketStatus.Inactive)
                {
                    SpanAttributes["data-title"] = MarketStatus.Inactive.ToString();
                    Color = "odds-bg-inactive";
                }
                else
                {
                    SpanAttributes["data-title"] = "Bet Stop";
                    Color = "odds-bg-betstop";
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
                            Color = "odds-bg-danger-active";
                        }

                        if (Convert.ToDecimal(Selection.OldDecimalOdds) < Convert.ToDecimal(Selection.DecimalOdds))
                        {
                            Color = "odds-bg-success-active";
                        }
                    }
                }
                else
                {
                    if (Selection.ClientOdds == null)
                        return Color;
                    var clientOdds = Selection.ClientOdds.Where(c => c.ClientCode == CurrentClientCode && c.SelectionTemplateId == Selection.SelectionTemplateId).FirstOrDefault();
                    if (clientOdds != null)
                    {
                        if (clientOdds.OldOdds != 0)
                        {
                            if (clientOdds.OldOdds > clientOdds.Odds)
                            {
                                Color = "odds-bg-danger-active";
                            }

                            if (clientOdds.OldOdds < clientOdds.Odds)
                            {
                                Color = "odds-bg-success-active";
                            }
                        }
                    }
                }
            }
            return Color;
        }

        #endregion
    }
}
