using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
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
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] private ToastService? _toasterService { get; set; }
        private ToastService toasterService => _toasterService!;
        public MarketSettlementModalFilterModel ModalFilter { get; set; } = new MarketSettlementModalFilterModel();
        public List<ManualMarketSettlementAuditModel> ManualMarketSettlements { get; set; }
        public MetaData MetaData { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public bool IsPreLoader { get; set; }

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 0,
                TotalPages = 1
            };

            ManualMarketSettlements = new List<ManualMarketSettlementAuditModel>();
            ModalFilter = new MarketSettlementModalFilterModel()
            {
                FromDate = DateTime.Now.Date.AddDays(-1),
                ToDate = DateTime.Now.Date.AddDays(1).AddTicks(-1),
            };

            PageTitle = "Market Settlement";
            IconClass = "ico ico-market-settlement";
            IsPreLoader = true;
        }

        public async Task GetContent()
        {
            var fromDate = Convert.ToDateTime(ModalFilter.FromDate.ToString("yyyy-MM-dd 00:00:00"));
            var toDate = Convert.ToDateTime(ModalFilter.ToDate.ToString("yyyy-MM-dd 23:59:59"));
            var filterSportsId = ModalFilter.SportId == 0 ? "%" : ModalFilter.SportId.ToString();
            var filterSegmentId = ModalFilter.SegmentId == 0 ? "%%" : ModalFilter.SegmentId.ToString();
            var filterLeagueId = ModalFilter.LeagueId == 0 ? "%%" : ModalFilter.LeagueId.ToString();
            var filterFixtureId = string.IsNullOrEmpty(ModalFilter.FixtureId) ? "%" : ModalFilter.FixtureId;
            
            var response = (await BackOfficeService.GetActivityLogs(new ActivityLogRequest
            {
                Page = AdminPermission.Admin_Settlements_MarketSettlement.ToString().Replace("_", "."),
                Payload = $"{filterSportsId}.{filterSegmentId}.{filterLeagueId}.{filterFixtureId}",
                MetaData = MetaData,
                DateFrom = Convert.ToDateTime(fromDate).Date,
                DateTo = Convert.ToDateTime(toDate).Date
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

            var marketIds = string.IsNullOrEmpty(ModalFilter.MarketId) ? new List<long>() : ModalFilter.MarketId.Split(",").Select(s => Convert.ToInt64(s)).ToList();
            var selectionIds = string.IsNullOrEmpty(ModalFilter.SelectionId) ? new List<long>() : ModalFilter.SelectionId.Split(",").Select(s => Convert.ToInt64(s)).ToList();

            ManualMarketSettlements = result.Where(x => (ModalFilter.ToStatus == 0 || x.ToStatusId == ModalFilter.ToStatus)
                                        && (string.IsNullOrEmpty(ModalFilter.MarketId) || x.MarketId.ToString() == ModalFilter.MarketId)
                                        && (string.IsNullOrEmpty(ModalFilter.SelectionId) || x.SelectionId.ToString() == ModalFilter.SelectionId)
                                        && (string.IsNullOrEmpty(ModalFilter.SettledBy) || x.ManualSettledBy.ToUpper().Contains(ModalFilter.SettledBy.ToUpper()))).ToList();


            IsPreLoader = false;
        }
        public async Task OnExportClickedEvent()
        {
            toasterService.AddToast(Toast.NewToast
                    ("Market Settlement",
                    $"Success! Excel file is being generated system will notify once done."));
        }
        public async Task SelectPage()
        {

        }
        public void Dispose()
        {
            
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
