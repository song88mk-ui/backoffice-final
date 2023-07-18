using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.OddsEngine.Monitoring;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;
using static Backoffice.Web.Models.ClientModel;

namespace Backoffice.Web.Components.Common
{
    public partial class FixtureRow : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }

        [Parameter] public List<Client> Clients { get; set; }
        [Parameter] public FixtureMarketModel Market { get; set; }
        [Parameter] public string Region { get; set; }
        [Parameter] public bool IsMonitoringPage { get; set; }

        #endregion

        #region Fields
        private string _mainMarketName = string.Empty;
        private bool _isBetStop = false;
        private bool _hasLine;
        private string _marketLine;
        private string _columnSize;
        private string _fixtureName;
        private int _fixtureStatusId;
        private string _currentPeriod;
        private string _currentMinute;
        private string _homeName;
        private string _awayName;
        private string _score;
        private DateTime _fixtureStartDate;
        private string _providerFixtureId;
        private int _marketStatusId;
        private string _clientCode;
        private string _clientCodeMargin;
        private List<SelectionModel> _selections;
        #endregion

        #region Methods
        protected override void OnParametersSet()
        {
            if (Market != null)
            {
                _fixtureName = Market.FixtureName;
                _fixtureStatusId = Market.FixtureStatusId;
                _currentPeriod = Market.CurrentPeriod;
                _currentMinute = Market.CurrentMinute;
                _homeName = Market.HomeName;
                _awayName = Market.AwayName;
                _score = Market.Score;
                _fixtureStartDate = Market.FixtureStartDate;
                _providerFixtureId = Market.ProviderFixtureId;
                _marketStatusId = Market.MarketStatusId;
                _clientCode = Market.ClientCode;
                _clientCodeMargin = Market.ClientCodeMargin;
                _selections = Market.Selections;

                if (_mainMarketName != Market.MarketName)
                {
                    _mainMarketName = Market.MarketName;

                    _hasLine = _mainMarketName.ToUpper().Contains("HANDICAP") || _mainMarketName.ToUpper().Contains("TOTAL");

                    var columns = 1;
                    columns += !_hasLine ? 0 : 1;
                    columns += Market != null ? Market.Selections.Count : 0;

                    // divide number of column size by 12 (max size of col in bootstrap)
                    _columnSize = $"col-xs-{12 / columns}";

                    if (_hasLine)
                        _marketLine = GetMarketLine(_mainMarketName, Market.Line);
                }

                if (Market.IsBetStopGlobal)
                {
                    _isBetStop = true;
                }
                else if (Market.BetStop != null)
                {
                    _isBetStop = Market.BetStop.Count > 0 && Market.BetStop.FirstOrDefault() == 1 ? true : false;
                }
            }
        }

        private void OnCloseMatchClickEvent()
        {
            ModalParameters parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-dialog-centered modal-lg");
            parameters.Add("FixtureName", _fixtureName);
            parameters.Add("ProviderFixtureId", _providerFixtureId);

            ModalOptions options = new ModalOptions();

            ModalService.Open<CloseMatchModal>("Close Match", parameters, options);
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities
        private string GetClientOdds(SelectionModel selection, string clientCode)
        {
            if (selection != null && selection.ClientOdds != null && selection.ClientOdds.Any(c => c.ClientCode == clientCode))
            {
                return selection.ClientOdds.FirstOrDefault(c => c.ClientCode == clientCode && c.SelectionTemplateId == selection.SelectionTemplateId)?.Odds.ToString() ?? string.Empty;
            }

            return selection?.DecimalOdds ?? string.Empty;
        }

        public string GetClientMargin(string clientCode, string clientCodeMargin, List<SelectionModel> selections)
        {
            var clients = string.IsNullOrEmpty(clientCodeMargin) ? new List<Tuple<string, decimal>>() : clientCodeMargin.Split('|').Select(s => new Tuple<string, decimal>(s.Split(':').First(), Convert.ToDecimal(s.Split(':').Last()))).ToList();
            var client = clients.FirstOrDefault(c => c.Item1 == clientCode);

            if (client == null)
            {
                var activeSelections = selections.Where(c => c.IsActive == true && Convert.ToDecimal(GetClientOdds(c, clientCode)) > 0m).ToList();
                var margin = (Math.Round(activeSelections.Sum(s => 100 / Convert.ToDecimal(GetClientOdds(s, clientCode)))) - 100) / 100;

                return ((int)((margin * 100) + 100)).ToString();
            }

            var newMargin = ((int)((client.Item2 * 100) + 100)).ToString();

            var newActiveSelections = selections.Where(c => c.IsActive == true).ToList();
            var computedMargin = $"{(Math.Round(newActiveSelections.Sum(s => 100 / s.ClientOdds.FirstOrDefault(c => c.ClientCode == clientCode && c.SelectionTemplateId == s.SelectionTemplateId)?.Odds ?? 1)) - 100) + 100}";

            if (newMargin != computedMargin)
            {
                newMargin = $"{newMargin} ({computedMargin})";
            }

            return newMargin;
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
        #endregion
    }
}
