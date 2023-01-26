using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;

namespace Backoffice.Web.Components.MonitoringDetails
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

        public void Dispose()
        {
        }

        #endregion
    }
}
