using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Backoffice.Web.Pages.Administration
{
    public partial class CancelBetHistory : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] private ToastService? ToasterService { get; set; }

        public CancelBetHistoryCallbackModel CallbackModel { get; set; }
        public CancelBetHistoryModel CancelBets { get; set; }
        public MetaData MetaData { get; set; }

        public bool IsPreload { get; set; }

        #endregion

        #region Fields
        private ToastService _toasterService => ToasterService!;
        private PageHeaderModel _pageHeaderModel;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            CallbackModel = new CancelBetHistoryCallbackModel();
            CancelBets = new CancelBetHistoryModel();

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 0,
                TotalPages = 1,
            };

            await PageState.AuthenticateAccessEvent.InvokeAsync();
            _pageHeaderModel = new()
            {
                Title = "Cancel Bet History",
                IconClass = "ico ico-cancel-bet-history"
            };
        }
        public async Task OnInitEvent(CancelBetHistoryCallbackModel request)
        {
            IsPreload = true;
            CallbackModel = request;

            CancelBets = await GetActivityLogList(request);
            IsPreload = false;
        }
        // BO-136: loads every row for the filters on screen and downloads them as a CSV.
        public async Task OnExportEvent(CancelBetHistoryCallbackModel model)
        {
            if (model == null)
                return;

            var (succeeded, message, bets, _) = await LoadCancelBets(model, new MetaData { CurrentPage = 0, PageSize = 1 });

            if (!succeeded)
            {
                _toasterService.AddToast(Toast.NewToast("Cancel Bet History", $"Export failed: {message}"));
                return;
            }

            if (!bets.Any())
            {
                _toasterService.AddToast(Toast.NewToast("Cancel Bet History", "There are no records to export for these filters."));
                return;
            }

            var fileName = HistoryExportCsvHelper.FileName("cancel-bet-history", DateTime.UtcNow);
            await _jsRuntime.InvokeVoidAsync("downloadFileFromText", fileName, HistoryExportCsvHelper.BuildCancelBetHistoryCsv(bets), "text/csv");
        }
        public async Task SelectPage(int page)
        {
            IsPreload = true;
            MetaData.CurrentPage = page;
            CancelBets = await GetActivityLogList(CallbackModel);
            IsPreload = false;
        }

        public void Dispose()
        {
            CancelBets = null;

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        private async Task<CancelBetHistoryModel> GetActivityLogList(CancelBetHistoryCallbackModel model)
        {
            var (succeeded, _, bets, metaData) = await LoadCancelBets(model, MetaData);

            if (succeeded)
                MetaData = metaData;

            return new CancelBetHistoryModel { CancelBets = bets };
        }

        // The table and the export ask for the same rows; the export passes CurrentPage 0, for which the API
        // returns every row instead of one page.
        private async Task<(bool Succeeded, string Message, List<CancelBetHistoryModel.BetDetails> Bets, MetaData MetaData)> LoadCancelBets(CancelBetHistoryCallbackModel model, MetaData metaData)
        {
            var bets = new List<CancelBetHistoryModel.BetDetails>();

            var response = await BackofficeService.GetActivityLogs(new ActivityLogRequest
            {
                Page = AdminPermission.Admin_CancelBets.ToString().Replace("_", "."),
                Payload = $"%{model.CancelledBy}%.%{model.BetReferenceId}%.%.%{model.MemberName}%.%{model.ClientName}%",
                DateFrom = model.DateFrom,
                DateTo = model.DateTo,
                MetaData = metaData
            });

            if (response?.StatusCode != StatusCode.Success || response.Result == null)
                return (false, response?.Message ?? "No response from the server.", bets, metaData);

            foreach (var item in response.Result.ActivityLogs)
            {
                var betDetails = JsonSerializer.Deserialize<CancelBetHistoryModel.BetDetails>(item.From);
                if (betDetails != null)
                {
                    betDetails.CancelledBy = item.UserName;

                    var betSelectionDetails = JsonSerializer.Deserialize<CancelBetHistoryModel.BetSelectionDetails>(item.To);
                    betDetails.Selections = new List<CancelBetHistoryModel.BetSelectionDetails>();

                    if (betSelectionDetails != null)
                        betDetails.Selections.Add(betSelectionDetails);

                    bets.Add(betDetails);
                }
            }

            return (true, response.Message, bets, response.Result.MetaData ?? metaData);
        }

        #endregion
    }
}
