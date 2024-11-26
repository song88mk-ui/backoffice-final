using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Runtime.Intrinsics.X86;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;

namespace Backoffice.Web.Components.OddsEngine.Monitoring
{
    public partial class MonitoringFixtureSelectionComponent : IDisposable
    {
        [Parameter] public ClientMarket ClientMarket {  get; set; }
        [Parameter] public MarketModelV2 BaseMarket { get; set; }

        private bool _hasLine;
        private int _marketStatusId;

        protected override Task OnParametersSetAsync(){

            _hasLine = BaseMarket.MarketName.Contains("HANDICAP", StringComparison.OrdinalIgnoreCase) || BaseMarket.MarketName.Contains("TOTAL", StringComparison.OrdinalIgnoreCase);

            _marketStatusId = GetMarketStatusId();

            return Task.CompletedTask;
        }

        public void Dispose() => throw new NotImplementedException();
        private int GetMarketStatusId()
        {
            if (ClientMarket == null || ClientMarket.Market.ClientCodeMargin <= 0)
                return (int)MarketStatus.Inactive;

            return ClientMarket.Market.MarketStatusId;
        }
        private string GetClientMargin(decimal clientCodeMargin, List<SelectionModel> selections)
        {

            if (ClientMarket.Market == null)
            {
                var activeSelections = selections.Where(c => c.IsActive == true && Convert.ToDecimal(c.DecimalOdds) > 0m).ToList();

                if (activeSelections.Count == 1)
                    activeSelections = selections.Where(c => Convert.ToDecimal(Convert.ToDecimal(c.DecimalOdds)) > 0m).ToList();

                var margin = (Math.Round(activeSelections.Sum(s => 100 / Convert.ToDecimal(s.DecimalOdds))) - 100) / 100;

                return ((int)((margin * 100) + 100)).ToString();
            }

            var newMargin = ((int)((ClientMarket.Market.ClientCodeMargin * 100) + 100)).ToString();

            var newActiveSelections = selections.Where(c => c.IsActive == true).ToList();

            if (newActiveSelections.Count == 1)
                newActiveSelections = selections.Where(c => c.IsActive == true).ToList();

            var computedMargin = "";
            try
            {
                computedMargin = $"{(Math.Round(newActiveSelections.Sum(s => 100 / s.ClientOdds.FirstOrDefault(c => c.ClientCode == clientCode && c.SelectionTemplateId == s.SelectionTemplateId)?.Odds ?? 1)) - 100) + 100}";
            }
            catch (Exception ex)
            {

            }
            if (newMargin != computedMargin)
            {
                newMargin = $"{newMargin} ({computedMargin})";
            }

            return newMargin;
        }
    }
}
