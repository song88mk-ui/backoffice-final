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
        [Parameter] public string MainMarketName { get; set; }
        [Parameter] public string Region { get; set; }
        [Parameter] public bool IsMonitoringPage { get; set; } 

        public string MarketLine { get; set; }
        public bool IsBetStop { get; set; }
        public bool HasLine { get; set; }
        public string ColumnSize { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            HasLine = MainMarketName.ToUpper().Contains("HANDICAP") || MainMarketName.ToUpper().Contains("TOTAL") ? true : false;

            var columns = 1;
            columns += !HasLine ? 0 : 1;
            columns += Market != null ? Market.Selections.Count : 0;

            // divide number of column size by 12 (max size of col in bootstrap)
            ColumnSize = $"col-xs-{12 / columns}";

            if (HasLine)
                GetMarketLine(MainMarketName);
        }
        protected override async Task OnParametersSetAsync()
        {
            IsBetStop = false;

            if (Market.IsBetStopGlobal)
            {
                IsBetStop = true;
            }
            else if (Market.BetStop != null)
            {
                IsBetStop = Market.BetStop.Count > 0 && Market.BetStop.FirstOrDefault() == 1 ? true : false;
            }
        }

        public async Task OnCloseMatchClickEvent()
        {
            ModalParameters parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-dialog-centered modal-lg");            

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
            if (selection.ClientOdds != null && selection.ClientOdds.Any(c => c.ClientCode == clientCode))
            {
                return selection.ClientOdds.Where(c => c.ClientCode == clientCode && c.SelectionTemplateId == selection.SelectionTemplateId).FirstOrDefault().Odds.ToString();
            }
            return selection.DecimalOdds;
        }

        public string GetClientMargin(string clientCode, string clientCodeMargin, List<SelectionModel> selections)
        {
            var clients = string.IsNullOrEmpty(clientCodeMargin) ? new List<Tuple<string, decimal>>() : clientCodeMargin.Split('|').Select(s => new Tuple<string, decimal>(s.Split(':').First(), Convert.ToDecimal(s.Split(':').Last()))).ToList();
            var client = clients.Where(c => c.Item1 == clientCode).FirstOrDefault();

            if (client == null)
            {
                var activeSelections = selections.Where(c => c.IsActive == true && Convert.ToDecimal(GetClientOdds(c, clientCode)) > 0m).ToList();
                var margin = (Math.Round(activeSelections.Sum(s => 100 / Convert.ToDecimal(GetClientOdds(s, clientCode)))) - 100) / 100;

                return ((int)((margin * 100) + 100)).ToString();
            }

            var newMargin = ((int)((client.Item2 * 100) + 100)).ToString();

            var newActiveSelections = selections.Where(c => c.IsActive == true).ToList();
            var computedMargin = $"{(Math.Round(newActiveSelections.Sum(s => 100 / s.ClientOdds.FirstOrDefault(c => c.ClientCode == clientCode && c.SelectionTemplateId == s.SelectionTemplateId).Odds)) - 100) + 100}";

            if (newMargin != computedMargin)
            {
                newMargin = $"{newMargin} ({computedMargin})";
            }

            return newMargin;
        }

        private void GetMarketLine(string marketName)
        {
            if (!string.IsNullOrEmpty(marketName))
            {
                if (Market?.Line == null)
                {
                    MarketLine = "-.-";
                }
                else
                {
                    if (marketName.ToUpper().Contains("TOTAL"))
                        MarketLine = $"o {decimal.Parse(Market?.Line)} u";

                    if (marketName.ToUpper().Contains("HANDICAP"))
                        MarketLine = $"h {decimal.Parse(Market?.Line)} a";
                }
            }
        }
        public bool HasClientConfig(string clientCode, string marketClientCode, string marketClientMargin)
        {
            if (string.IsNullOrEmpty(marketClientCode))
                marketClientCode = string.Empty;

            if (string.IsNullOrEmpty(marketClientMargin))
                marketClientMargin = string.Empty;

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
