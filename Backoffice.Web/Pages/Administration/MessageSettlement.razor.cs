using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Administration.MessageSettlement;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Backoffice.Web.Pages.Administration
{
    public partial class MessageSettlement : IDisposable
    {
        #region Properties

        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] private ToastService? ToasterService { get; set; }

        public MarketSettlementFilterModel FilterModel { get; set; } = new MarketSettlementFilterModel();
        public List<MessageSettlementModel> MessageSettlementModel { get; set; }
        public MetaData MetaData { get; set; }

        #endregion

        #region Fields
        private ToastService _toasterService => ToasterService!;
        private PageHeaderModel _pageHeaderModel;
        private bool IsPreLoader { get; set; }
        private long CursorId { get; set; }
        private int PreviousPage { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            IsPreLoader = true;
            _pageHeaderModel = new()
            {
                Title = "Message Settlement",
                IconClass = "ico ico-message-settlement"
            };

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 100,
                TotalCount = 0,
                TotalPages = 1,
            };

            await PageState.AuthenticateAccessEvent.InvokeAsync();
        }

        public async Task OnAddClickedEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("DialogStyle", "max-width: 80%; overflow-y: initial !important");

            var options = new ModalOptions();

            ModalService.Open<AddMessageSettlement>("Add Message Settlement", parameters, options);
        }

        public async Task OnSaveClickedEvent()
        {
            await GetContents();
        }

        // BO-136: loads every row for the filters on screen and downloads them as a CSV.
        public async Task OnExportClickedEvent()
        {
            // CurrentPage 0: the API returns every row instead of one page.
            var logs = await LoadLogs(new MetaData { CurrentPage = 0, PageSize = 1 });

            if (logs?.StatusCode != StatusCode.Success || logs.Result == null)
            {
                _toasterService.AddToast(Toast.NewToast("Message Settlement", $"Export failed: {logs?.Message ?? "No response from the server."}"));
                return;
            }

            var settlements = ToModels(logs.Result.ActivityLogs);

            if (!settlements.Any())
            {
                _toasterService.AddToast(Toast.NewToast("Message Settlement", "There are no records to export for these filters."));
                return;
            }

            var fileName = HistoryExportCsvHelper.FileName("message-settlement-history", DateTime.UtcNow);
            await JSRuntime.InvokeVoidAsync("downloadFileFromText", fileName, HistoryExportCsvHelper.BuildMessageSettlementCsv(settlements), "text/csv");
        }

        public async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;

            await GetContents();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utulities

        public async Task GetContents()
        {
            IsPreLoader = true;

            var logs = await LoadLogs(MetaData);

            var result = logs?.Result?.ActivityLogs != null ? ToModels(logs.Result.ActivityLogs) : new List<MessageSettlementModel>();

            MetaData = logs?.Result?.MetaData;
            CursorId = result?.LastOrDefault()?.Id ?? 0;
            PreviousPage = MetaData.CurrentPage;
            IsPreLoader = false;
            MessageSettlementModel = result;
            StateHasChanged();
        }

        // The table and the export ask for the same rows; the export passes CurrentPage 0.
        private Task<BaseResponse<ActivityLogResponse>> LoadLogs(MetaData metaData)
        {
            return BackofficeService.GetActivityLogs(new ActivityLogRequest()
            {
                Page = AdminPermission.Admin_Settlements_MessageSettlement.ToString().Replace("_", "."),
                Payload = BuildPayload(),
                MetaData = metaData,
                DateFrom = FilterModel.FromDate,
                DateTo = FilterModel.ToDate
            });
        }

        private List<MessageSettlementModel> ToModels(IEnumerable<ActivityLogModel> logs)
        {
            var products = GetProducts();

            return logs.Select(x =>
            {
                var to = JsonSerializer.Deserialize<MessageSettlementTo>(x.To);
                return new MessageSettlementModel()
                {
                    Id = x.Id,
                    BetDate = to.BetDate,
                    BetId = to.BetId,
                    Sports = to.SportName,
                    MessagesSettled = to.FixtureName,
                    Product = products.FirstOrDefault(y => y.Value == to.ProductId)?.Name,
                    SettledBy = x.UserName,
                    DateSettled = x.TimeStamp,
                    Reason = to.Reason
                };
            }).ToList();
        }

        private string BuildPayload()
        {
            return $"{(FilterModel.SportId > 0 ? FilterModel.SportId : "%")}." +
                $"{(FilterModel.SegmentId > 0 ? FilterModel.SegmentId : "%")}." +
                $"{(FilterModel.LeagueId > 0 ? FilterModel.LeagueId : "%")}." +
                $"{(!string.IsNullOrEmpty(FilterModel.FixtureId) ? FilterModel.FixtureId : "%")}";
        }

        private List<SelectListItemModel<int>> GetProducts()
        {
            return new List<SelectListItemModel<int>>()
            {
                new SelectListItemModel<int> { Value = 1, Name = "Live Odds" },
                new SelectListItemModel<int> { Value = 3, Name = "Prematch Odds (CTRL)" },
                new SelectListItemModel<int> { Value = 4, Name = "betpal" },
                new SelectListItemModel<int> { Value = 5, Name = "Premium Cricket" },
                new SelectListItemModel<int> { Value = 6, Name = "Virtual Soccer" },
                new SelectListItemModel<int> { Value = 7, Name = "wns" },
                new SelectListItemModel<int> { Value = 8, Name = "Virtual Basketball" },
                new SelectListItemModel<int> { Value = 9, Name = "vto" },
                new SelectListItemModel<int> { Value = 10, Name = "Virtual Dog Racing" },
                new SelectListItemModel<int> { Value = 11, Name = "Virtual Horses" },
                new SelectListItemModel<int> { Value = 12, Name = "Virtual Tennis" },
                new SelectListItemModel<int> { Value = 14, Name = "codds" },
                new SelectListItemModel<int> { Value = 15, Name = "Virtual Baseball" },
                new SelectListItemModel<int> { Value = 17, Name = "Virtual Cricket" },
            };
        }

        #endregion
    }
}
