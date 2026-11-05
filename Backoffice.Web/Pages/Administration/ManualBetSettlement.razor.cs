using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text;
using System.Text.Json;
using static Backoffice.Shared.Models.ManualBetDetailsModel;

namespace Backoffice.Web.Pages.Administration
{
    public partial class ManualBetSettlement : IDisposable
    {
        #region Properties

        [Inject] private IBackofficeService BackofficeService { get; set; }
        [Inject] private ToastService? ToasterService { get; set; }

        public List<ActivityLogModel> ActivityLogs { get; set; }
        public List<AlertModel> Alerts { get; set; }

        public MetaData MetaData { get; set; }
        public ManualBetSettlementCallbackModel CallbackModel { get; set; }
        private bool IsPreloader { get; set; }
        private bool IsExportDisabled { get; set; }
        private Guid Guid { get; set; }

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
                Title = "Manual Bet Settlement",
                IconClass = "ico ico-manual-bet-settlement"
            };

            Alerts = new List<AlertModel>();
            IsPreloader = true;
            IsExportDisabled = true;
            Guid = new Guid();

            CallbackModel = new ManualBetSettlementCallbackModel();

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 0,
                TotalPages = 1,
            };

            await PageState.AuthenticateAccessEvent.InvokeAsync();

            await GetContents();
        }

        public async Task OnSearchButtonClicked()
        {
            //
            //IsPreloader = true;
            //StateHasChanged();
            await GetContents();
        }

        public async Task OnConfirmBetSettlement(object responseObj)
        {
            var response = (BaseResponse<ManualBetSettlementResponse>)responseObj;
            Alerts = new List<AlertModel>();

            if (response.StatusCode == StatusCode.Success)
            {
                Alerts.Add(new AlertModel()
                {
                    Status = AlertStatus.Success,
                    Message = "BetId's have been successfully settled",
                    Opacity = 1.0m
                });

                Guid = Guid.NewGuid();
            }
            else
            {
                Alerts.Add(new AlertModel()
                {
                    Status = AlertStatus.Error,
                    Message = response.Message,
                    Opacity = 1.0m
                });

                Guid = Guid.NewGuid();
            }

            await GetContents();
        }

        // BO-136: loads every row for the filters on screen and downloads them as a CSV.
        public async Task OnExportExcel()
        {
            var response = await BackofficeService.GetManualBetSettlementActivityLogs(new ManualBetSettlementActivityLogRequest()
            {
                ManualBetSettlementCallbackModel = CallbackModel,
                // CurrentPage 0: the API returns every row instead of one page.
                MetaData = new MetaData { CurrentPage = 0, PageSize = 1 }
            });

            if (response?.StatusCode != StatusCode.Success || response.Result == null)
            {
                _toasterService.AddToast(Toast.NewToast("Manual Bet Settlement", $"Export failed: {response?.Message ?? "No response from the server."}"));
                return;
            }

            // The same rows the table shows: a log whose details cannot be read is skipped there too.
            var rows = new List<(ManualBetDetails Bet, string SettledBy)>();
            foreach (var log in response.Result.ActivityLogs ?? new List<ActivityLogModel>())
            {
                var betDetails = ReadBetDetails(log);
                if (betDetails != null)
                    rows.Add((betDetails, log.UserName));
            }

            if (!rows.Any())
            {
                _toasterService.AddToast(Toast.NewToast("Manual Bet Settlement", "There are no records to export for these filters."));
                return;
            }

            var fileName = HistoryExportCsvHelper.FileName("manual-bet-settlement-history", DateTime.UtcNow);
            await _jsRuntime.InvokeVoidAsync("downloadFileFromText", fileName, HistoryExportCsvHelper.BuildManualBetSettlementCsv(rows), "text/csv");
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

        #region Utilities

        private async Task OnInitEvent(ManualBetSettlementCallbackModel callbackModel)
        {
            CallbackModel = callbackModel;
        }

        private async Task GetContents()
        {
            IsPreloader = true;
            IsExportDisabled = true;

            var response = await BackofficeService.GetManualBetSettlementActivityLogs(new ManualBetSettlementActivityLogRequest()
            {
                ManualBetSettlementCallbackModel = CallbackModel,
                MetaData = MetaData
            });

            if (response.Result?.ActivityLogs?.Any() ?? false)
            {
                IsExportDisabled = false;
            }

            ActivityLogs = response.Result?.ActivityLogs?.ToList() ?? new List<ActivityLogModel>();
            MetaData = response.Result?.MetaData ?? MetaData;

            IsPreloader = false;

        }

        private string BuildEventDetails(BetSelectionDetails betSelectionDetails, string oddsFormat)
        {
            if (betSelectionDetails == null) return string.Empty;

            StringBuilder builder = new StringBuilder();

            builder.Append($"<span class=\"segment-league d-block\">{MarkupHelper.Encode(betSelectionDetails.LeagueName)}</span>");
            builder.Append($"<span class=\"fixture d-block\">{MarkupHelper.Encode(betSelectionDetails.FixtureName)}</span>");
            builder.Append($"<span class=\"start-date d-block\">{betSelectionDetails.FixtureStartDate.ToString("MM/dd/yyyy hh:mm:ss tt").ToUpper()}</span>");
            builder.Append($"<span class=\"selection-details d-block\">{MarkupHelper.Encode(betSelectionDetails.SelectionName)}</span>");
            builder.Append($"<span class=\"market-name d-block\">{MarkupHelper.Encode(betSelectionDetails.MarketName)} @ {betSelectionDetails.Odds} ({MarkupHelper.Encode(oddsFormat)})</span>");
            builder.Append($"<span class=\"score d-block\">{MarkupHelper.Encode(betSelectionDetails.BetScore)}, Final Score: {MarkupHelper.Encode(betSelectionDetails.SettlementScore)}</span>");


            return builder.ToString();
        }

        public string GetBetStatusName(ManualBetDetails betDetails) => HistoryExportCsvHelper.ToStatusName(betDetails);

        private static ManualBetDetails? ReadBetDetails(ActivityLogModel log)
        {
            try
            {
                return JsonSerializer.Deserialize<ManualBetDetails>(log.To);
            }
            catch (JsonException)
            {
                return null;
            }
        }
        #endregion
    }
}
