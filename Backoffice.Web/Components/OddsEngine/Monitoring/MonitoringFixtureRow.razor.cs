using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Monitoring;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Response.FastOddsFixturesResponse;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;

namespace Backoffice.Web.Components.OddsEngine.Monitoring
{
    public partial class MonitoringFixtureRow : IDisposable
    {
        #region Properties
        [Parameter] public RenderFragment? FixtureHeaderContent { get; set; }
        [Parameter] public long UnixTime { get; set; }
        [Parameter] public bool IsBetStop { get; set; }
        [Parameter] public List<Client> Clients { get; set; }
        [Parameter] public List<MonitoringFixtureClient> Markets { get; set; }

        #endregion

        #region Fields

        private string _mainMarketName = string.Empty;
        private bool _isBetStop = false;
        private bool _hasLine;


        private bool _init = true;
        private bool _shouldRender = true;

        private long _unixTime;
        private string _clientReference;

        private MonitoringFixtureClient _ctrlMarket { get; set; }
        private List<MonitoringFixtureClient> _clientList = new List<MonitoringFixtureClient>();

        #endregion

        #region Methods

        protected override void OnParametersSet()
        {
            _shouldRender = ValidateParameters();

            if (_shouldRender)
            {
                _isBetStop = IsBetStop;
                _clientReference = string.Join(',', Clients.Select(s => s.ClientCode).OrderBy(c => c).Select(s => s.Trim()).ToList());
                _unixTime = UnixTime;
                _ctrlMarket = Markets.FirstOrDefault(c => c.ClientCode.Equals("CTRL", StringComparison.OrdinalIgnoreCase)) ?? new MonitoringFixtureClient();

                _clientList = Clients
                    .GroupJoin(
                        Markets,
                        client => client.ClientCode,
                        market => market.ClientCode,
                        (client, markets) => new { Client = client, Markets = markets.DefaultIfEmpty() }
                    )
                    .SelectMany(
                        group => group.Markets.Select(market => new MonitoringFixtureClient
                        {
                            ClientCode = group.Client.ClientCode,
                            UnixTime = market?.UnixTime ?? 0,
                            Market = market?.Market
                        })
                    )
                    .OrderBy(c => Clients.FindIndex(client => client.ClientCode == c.ClientCode))
                    .ToList();

                //if (_mainMarketName != Market.MarketName)
                //{
                //    _mainMarketName = Market.MarketName;

                //    _hasLine = _mainMarketName.ToUpper().Contains("HANDICAP") || _mainMarketName.ToUpper().Contains("TOTAL");

                //    var columns = 1;
                //    columns += !_hasLine ? 0 : 1;
                //    columns += Market != null ? Market.Selections.Count : 0;

                //    _columnSize = $"col-xs-{12 / columns}";

                //    if (_hasLine)
                //        _marketLine = GetMarketLine(_mainMarketName, Market.Line);
                //}

                _init = false;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender) => _shouldRender = false;

        protected override bool ShouldRender() => _shouldRender;

        public string GetClientMargin(string clientCode, string clientCodeMargin, List<SelectionModel> selections)
        {
            var clients = string.IsNullOrEmpty(clientCodeMargin) ? new List<Tuple<string, decimal>>() : clientCodeMargin.Split('|').Select(s => new Tuple<string, decimal>(s.Split(':').First(), Convert.ToDecimal(s.Split(':').Last()))).ToList();
            var client = clients.FirstOrDefault(c => c.Item1 == clientCode);

            if (client == null)
            {
                var activeSelections = selections.Where(c => c.IsActive == true && Convert.ToDecimal(GetClientOdds(c, clientCode)) > 0m).ToList();

                if (activeSelections.Count == 1)
                    activeSelections = selections.Where(c => Convert.ToDecimal(GetClientOdds(c, clientCode)) > 0m).ToList();

                var margin = (Math.Round(activeSelections.Sum(s => 100 / Convert.ToDecimal(GetClientOdds(s, clientCode)))) - 100) / 100;

                return ((int)((margin * 100) + 100)).ToString();
            }

            var newMargin = ((int)((client.Item2 * 100) + 100)).ToString();

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

        public bool HasClientConfig(string clientCode, string marketClientCode, string marketClientMargin)
        {
            if (marketClientCode.Split("|").Contains(clientCode) || marketClientMargin.Contains(clientCode))
                return true;

            return false;
        }

        public int GetMarketStatusId(Client client, string marketClientCodes, int marketStatusId)
        {
            var result = marketStatusId;

            if (string.IsNullOrEmpty(marketClientCodes))
            {
                marketClientCodes = string.Empty;
            }

            var inactiveMarketClients = marketClientCodes.Split("|");

            if (!inactiveMarketClients.Contains(client.ClientCode))
            {
                result = (int)MarketStatus.Inactive;
            }

            return result;
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private string GetClientMargin(decimal clientCodeMargin, List<MonitoringFixtureSelectionModel> selections)
        {
            if (clientCodeMargin > 0)
            {
                var newMargin = ((int)((clientCodeMargin * 100) + 100)).ToString();

                var newActiveSelections = selections.Where(c => c.IsActive == true).ToList();

                if (newActiveSelections.Count == 1)
                    newActiveSelections = selections.Where(c => c.IsActive == true).ToList();

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

            var activeSelections = selections.Where(c => c.IsActive == true && Convert.ToDecimal(c.DecimalOdds) > 0m).ToList();

            if (activeSelections.Count == 1)
                activeSelections = selections.Where(c => Convert.ToDecimal(Convert.ToDecimal(c.DecimalOdds)) > 0m).ToList();

            var margin = (Math.Round(activeSelections.Sum(s => 100 / Convert.ToDecimal(s.DecimalOdds))) - 100) / 100;

            return ((int)((margin * 100) + 100)).ToString();
        }
        private bool ValidateParameters()
        {
            var clientReference = string.Join(',', Clients.Select(s => s.ClientCode).OrderBy(c => c).Select(s => s.Trim()).ToList());

            return (_init || _unixTime != UnixTime || _clientReference != clientReference || _isBetStop != IsBetStop) && Markets != null;
        }

        private string GetClientOdds(SelectionModel selection, string clientCode)
        {
            if (selection != null && selection.ClientOdds != null && selection.ClientOdds.Any(c => c.ClientCode == clientCode))
            {
                return selection.ClientOdds.FirstOrDefault(c => c.ClientCode == clientCode && c.SelectionTemplateId == selection.SelectionTemplateId)?.Odds.ToString() ?? string.Empty;
            }

            return selection?.DecimalOdds ?? string.Empty;
        }

        private string GetMarketLine(string marketName, string marketLine)
        {
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

        #endregion
    }
}
