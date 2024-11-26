using Backoffice.Shared.Enums;
using Backoffice.Web.Models.OddsEngine.Monitoring;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Azure;

namespace Backoffice.Web.Components.OddsEngine.Monitoring
{
    public partial class MonitoringFixtureSelectionComponent : IDisposable
    {
        [Parameter] public MonitoringFixtureMarketModel Market { get; set; }
        [Parameter] public bool IsBetStop { get; set; }

        private string _ctrlCode = "CTRL";
        private bool _hasLine;
        private int _marketStatusId;
        private string _clientMargin = "-.-";
        private bool _isBetStop;
        private string _marketLine = "-.-";

        private List<MonitoringFixtureSelectionModel> _selections;

        protected override Task OnParametersSetAsync()
        {

            _hasLine = Market.MarketName.Contains("HANDICAP", StringComparison.OrdinalIgnoreCase) || Market.MarketName.Contains("TOTAL", StringComparison.OrdinalIgnoreCase);

            var hasClientConfig = true;
            if (Market.ClientCode != _ctrlCode)
                hasClientConfig = Market.ClientCodeMargin > 0;

            _isBetStop = IsBetStop;
            _selections = Market.Selections;
            _marketStatusId = GetMarketStatusId();

            if (hasClientConfig)
            {
                _marketLine = GetMarketLine();
                _clientMargin = GetClientMargin(Market.ClientCodeMargin);
            }
            else
            {
                _selections.ForEach(selection => selection.DecimalOdds = "-.-");
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
        private int GetMarketStatusId()
        {
            if (Market.ClientCode != _ctrlCode && Market.ClientCodeMargin <= 0)
                return (int)MarketStatus.Inactive;

            return Market.MarketStatusId;
        }
        private string GetClientMargin(decimal clientCodeMargin)
        {
            if (clientCodeMargin > 0)
            {
                var newMargin = ((int)((Market.ClientCodeMargin * 100) + 100)).ToString();

                var newActiveSelections = _selections.Where(c => c.IsActive == true).ToList();

                if (newActiveSelections.Count == 1)
                    newActiveSelections = _selections.Where(c => c.IsActive == true).ToList();

                var computedMargin = "";
                try
                {
                    computedMargin = $"{(Math.Round(newActiveSelections.Sum(s => 100 / Convert.ToDecimal(s.DecimalOdds))) - 100) + 100}";
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

            var activeSelections = _selections.Where(c => c.IsActive == true && Convert.ToDecimal(c.DecimalOdds) > 0m).ToList();

            if (activeSelections.Count == 1)
                activeSelections = _selections.Where(c => Convert.ToDecimal(Convert.ToDecimal(c.DecimalOdds)) > 0m).ToList();

            var margin = (Math.Round(activeSelections.Sum(s => 100 / Convert.ToDecimal(s.DecimalOdds))) - 100) / 100;

            return ((int)((margin * 100) + 100)).ToString();
        }
        private string GetMarketLine()
        {
            var marketName = Market.MarketName;
            var marketLine = Market.Line;

            var result = string.Empty;
            if (!string.IsNullOrEmpty(marketName))
            {
                if (marketLine == null)
                {
                    result = "-.-";
                }
                else
                {
                    if (marketName.ToUpper().Contains("TOTAL"))
                        result = $"o {decimal.Parse(marketLine ?? string.Empty)} u";

                    if (marketName.ToUpper().Contains("HANDICAP"))
                        result = $"h {decimal.Parse(marketLine ?? string.Empty)} a";
                }
            }
            return result;
        }
    }
}
