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
        [Parameter] public ClientModel ClientModel { get; set; }

        #endregion

        #region Methods

        public string GetClientOdds(string clientCode)
        {
            if (Selection.ClientOdds != null && Selection.ClientOdds.Any(c => c.ClientCode == clientCode))
            {
                return Selection.ClientOdds.Where(c => c.ClientCode == clientCode && c.SelectionTemplateId == Selection.SelectionTemplateId).FirstOrDefault().Odds.ToString("0.00");
            }

            return Selection.DecimalOdds;
        }

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

        public string GetClientColor(string ClientCode = null)
        {
            var color = "odds-bg-default";

            if (Market.IsBetStopGlobal || Market.MarketStatusId != (int)MarketStatus.Active)
            {
                if (Market.MarketStatusId == (int)MarketStatus.Inactive)
                {
                    color = "odds-bg-inactive";
                }
                else
                {
                    color = "odds-bg-betstop";
                }
            }
            else
            {
                if (ClientCode == null)
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
                    if (Selection.ClientOdds == null)
                        return color;

                    var clientOdds = Selection.ClientOdds.FirstOrDefault(c => c.ClientCode == ClientCode && c.SelectionTemplateId == Selection.SelectionTemplateId);

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

            return color;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
