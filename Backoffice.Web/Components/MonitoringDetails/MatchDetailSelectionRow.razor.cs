using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using System.Drawing;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;

namespace Backoffice.Web.Components.MonitoringDetails
{
    public partial class MatchDetailSelectionRow : IDisposable
    {
        #region Properties
        [Parameter] public SelectionModel Selection { get; set; }
        [Parameter] public MarketModel Market { get; set; }
        [Parameter] public ClientModel ClientModel { get; set; }
        #endregion


        public void Dispose()
        {
        }

        #region Utilities
        public string GetClientOdds(string clientCode)
        {
            if (Selection.ClientOdds != null && Selection.ClientOdds.Any(c => c.ClientCode == clientCode))
            {
                return Selection.ClientOdds.Where(c => c.ClientCode == clientCode && c.SelectionTemplateId == Selection.SelectionTemplateId).FirstOrDefault().Odds.ToString("0.00");
            }

            return Selection.DecimalOdds;
        }

        protected bool HasClientConfig(string clientCode, string marketClientCode, string marketClientMargin)
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
        #endregion
    }
}
