using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
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

            var (succeeded, _, settlements, metaData) = await LoadSettlements(MetaData);

            if (succeeded)
                MetaData = metaData;

            ManualMarketSettlements = settlements;

            IsPreLoader = false;
        }

        // BO-136: loads every row for the filters on screen and downloads them as a CSV.
        public async Task OnExportClickedEvent()
        {
            var (succeeded, message, settlements, _) = await LoadSettlements(new MetaData { CurrentPage = 0, PageSize = 1 });

            if (!succeeded)
            {
                _toasterService.AddToast(Toast.NewToast("Market Settlement", $"Export failed: {message}"));
                return;
            }

            if (!settlements.Any())
            {
                _toasterService.AddToast(Toast.NewToast("Market Settlement", "There are no records to export for these filters."));
                return;
            }

            var fileName = HistoryExportCsvHelper.FileName("market-settlement-history", DateTime.UtcNow);
            await _jsRuntime.InvokeVoidAsync("downloadFileFromText", fileName, HistoryExportCsvHelper.BuildMarketSettlementCsv(settlements), "text/csv");
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
        protected string GetMarketLine(string marketName, string line) => HistoryExportCsvHelper.MarketLine(marketName, line);

        // The full settlement detail is stored in the activity log's Payload column (nvarchar(max)); fetch all
        // entries for the page/date range and apply the fixture/market/selection filters to the deserialized
        // record. The table and the export ask for the same rows; the export passes CurrentPage 0, for which the
        // API returns every row instead of one page.
        private async Task<(bool Succeeded, string Message, List<ManualMarketSettlementAuditModel> Settlements, MetaData MetaData)> LoadSettlements(MetaData metaData)
        {
            var response = await BackofficeService.GetMarketSettlementActivityLogs(new ActivityLogRequest
            {
                Page = AdminPermission.Admin_Settlements_MarketSettlement.ToString().Replace("_", "."),
                Payload = "%",
                MetaData = metaData,
                DateFrom = ModalFilterModel.FromDate,
                DateTo = ModalFilterModel.ToDate
            });

            if (response?.StatusCode != StatusCode.Success || response.Result == null)
                return (false, response?.Message ?? "No response from the server.", new List<ManualMarketSettlementAuditModel>(), metaData);

            var result = response.Result.ActivityLogs
                .Select(item => JsonConvert.DeserializeObject<ManualMarketSettlementAuditModel>(item.Payload))
                .Where(x => x != null);

            var settlements = result.Where(x => (ModalFilterModel.SportId == 0 || x.SportId == ModalFilterModel.SportId)
                                        && (ModalFilterModel.SegmentId == 0 || x.SegmentId == ModalFilterModel.SegmentId)
                                        && (ModalFilterModel.LeagueId == 0 || x.LeagueId == ModalFilterModel.LeagueId)
                                        && (string.IsNullOrEmpty(ModalFilterModel.FixtureId) || x.FixtureId.ToString() == ModalFilterModel.FixtureId)
                                        && (ModalFilterModel.ToStatus == 0 || x.ToStatusId == ModalFilterModel.ToStatus)
                                        && (string.IsNullOrEmpty(ModalFilterModel.MarketId) || x.MarketId.ToString() == ModalFilterModel.MarketId)
                                        && (string.IsNullOrEmpty(ModalFilterModel.SelectionId) || x.SelectionId.ToString() == ModalFilterModel.SelectionId)
                                        && (string.IsNullOrEmpty(ModalFilterModel.SettledBy) || (x.ManualSettledBy ?? string.Empty).ToUpper().Contains(ModalFilterModel.SettledBy.ToUpper()))).ToList();

            return (true, response.Message, settlements, response.Result.MetaData ?? metaData);
        }
        #endregion
    }
}
