using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

namespace Backoffice.Web.Pages.Administration
{
    public partial class MarketSettlement : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] private ToastService? ToasterService { get; set; }

        

        public MarketSettlementModalFilterModel ModalFilterModel { get; set; } = new MarketSettlementModalFilterModel();
        public List<ManualMarketSettlementAuditModel> ManualMarketSettlements { get; set; }
        public MetaData MetaData { get; set; }

        private bool IsPreLoader { get; set; }

        #endregion

        #region Fields
        private ToastService _toasterService => ToasterService!;
        private PageHeaderModel _pageHeaderModel;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            _pageHeaderModel = new()
            {
                Title = "Market Settlement",
                IconClass = "ico ico-market-settlement"
            };
            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 0,
                TotalPages = 1
            };

            ManualMarketSettlements = new List<ManualMarketSettlementAuditModel>();
            ModalFilterModel = new MarketSettlementModalFilterModel()
            {
                FromDate = DateTimeHelper.Now(_userState.TimeZoneOffset).Date.AddDays(-1),
                ToDate = DateTimeHelper.Now(_userState.TimeZoneOffset).Date.AddDays(1).AddTicks(-1),
            };

            await PageState.AuthenticateAccessEvent.InvokeAsync();
            await GetContent();
        }

        public async Task GetContent()
        {
            IsPreLoader = true;

            var filterSportsId = ModalFilterModel.SportId == 0 ? "%" : ModalFilterModel.SportId.ToString();
            var filterSegmentId = ModalFilterModel.SegmentId == 0 ? "%%" : ModalFilterModel.SegmentId.ToString();
            var filterLeagueId = ModalFilterModel.LeagueId == 0 ? "%%" : ModalFilterModel.LeagueId.ToString();
            var filterFixtureId = string.IsNullOrEmpty(ModalFilterModel.FixtureId) ? "%" : ModalFilterModel.FixtureId;

            var response = (await BackofficeService.GetActivityLogs(new ActivityLogRequest
            {
                Page = AdminPermission.Admin_Settlements_MarketSettlement.ToString().Replace("_", "."),
                Payload = $"{filterSportsId}.{filterSegmentId}.{filterLeagueId}.{filterFixtureId}",
                MetaData = MetaData,
                DateFrom = ModalFilterModel.FromDate,
                DateTo = ModalFilterModel.ToDate
            })).Result;

            List<ManualMarketSettlementAuditModel> result = new List<ManualMarketSettlementAuditModel>();

            if (response != null)
            {
                MetaData = response.MetaData;
                foreach (var item in response.ActivityLogs)
                {
                    result.Add(JsonConvert.DeserializeObject<ManualMarketSettlementAuditModel>(item.To));
                }
            }

            var marketIds = string.IsNullOrEmpty(ModalFilterModel.MarketId) ? new List<long>() : ModalFilterModel.MarketId.Split(",").Select(s => Convert.ToInt64(s)).ToList();
            var selectionIds = string.IsNullOrEmpty(ModalFilterModel.SelectionId) ? new List<long>() : ModalFilterModel.SelectionId.Split(",").Select(s => Convert.ToInt64(s)).ToList();

            ManualMarketSettlements = result.Where(x => (ModalFilterModel.ToStatus == 0 || x.ToStatusId == ModalFilterModel.ToStatus)
                                        && (string.IsNullOrEmpty(ModalFilterModel.MarketId) || x.MarketId.ToString() == ModalFilterModel.MarketId)
                                        && (string.IsNullOrEmpty(ModalFilterModel.SelectionId) || x.SelectionId.ToString() == ModalFilterModel.SelectionId)
                                        && (string.IsNullOrEmpty(ModalFilterModel.SettledBy) || x.ManualSettledBy.ToUpper().Contains(ModalFilterModel.SettledBy.ToUpper()))).ToList();

            IsPreLoader = false;
        }

        public async Task OnExportClickedEvent()
        {
            _toasterService.AddToast(Toast.NewToast
                    ("Market Settlement",
                    $"Success! Excel file is being generated system will notify once done."));
        }

        public async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;

            await GetContent();
            //ClientListModel = await GetClientList(ClientListCallbackModel.SearchText, ClientListCallbackModel.SortBy, MetaData.CurrentPage);
        }

        public void Dispose()
        {
            ModalFilterModel = null;
            ManualMarketSettlements = null;
            MetaData = null;

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities
        public string GetStatusClass(int statusId)
        {
            string className = "text-success";
            switch (statusId)
            {
                case (int)BetStatus.HalfLost:
                case (int)BetStatus.Lost:
                    className = "text-danger";
                    break;
                case (int)BetStatus.Void:
                    className = "text-warning";
                    break;
                default:
                    className = "text-success";
                    break;
            }
            return className;
        }
        protected string GetMarketLine(string marketName, string line)
        {
            if (!string.IsNullOrEmpty(marketName))
            {
                if (string.IsNullOrEmpty(line))
                {
                    return string.Empty;
                }
                else
                {
                    if (marketName.ToUpper().Contains("TOTAL"))
                        return $"o {decimal.Parse(line)} u";

                    if (marketName.ToUpper().Contains("HANDICAP"))
                        return $"h {decimal.Parse(line)} a";
                }
            }
            return string.Empty;
        }
        #endregion
    }
}
