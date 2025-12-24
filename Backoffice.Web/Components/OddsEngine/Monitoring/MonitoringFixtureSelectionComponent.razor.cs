using Backoffice.Shared.Enums;
using Backoffice.Web.Models.OddsEngine.Monitoring;
using Microsoft.AspNetCore.Components;

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
            var hasClientConfig = true;
            if (Market.ClientCode != _ctrlCode)
                hasClientConfig = Market.ClientCodeMargin > 0;

            _isBetStop = IsBetStop;
            _selections = Market.Selections;
            _marketStatusId = GetMarketStatusId();

            if (hasClientConfig)
            {
                _marketLine = GetMarketLine();
                //_clientMargin = Market.StrClientCodeMargin;
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
