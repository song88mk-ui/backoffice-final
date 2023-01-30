using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Pages.Administration
{
    public partial class CancelBetHistory : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        public CancelBetHistoryCallbackModel CallbackModel { get; set; }
        public CancelBetHistoryModel CancelBets { get; set; }
        public MetaData MetaData { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            CallbackModel = new CancelBetHistoryCallbackModel();
            CancelBets = new CancelBetHistoryModel();

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 20,
                TotalCount = 0,
                TotalPages = 1,
            };

            PageTitle = "Cancel Bet History";
            IconClass = "ico ico-cancel-bet-history";
        }
        public async Task OnInitEvent(CancelBetHistoryCallbackModel request)
        {
            CallbackModel = request;

            CancelBets = await GetActivityLogList(request);
        }

        public void Dispose()
        {
        }

        #endregion
        #region Utilities
        private async Task<CancelBetHistoryModel> GetActivityLogList(CancelBetHistoryCallbackModel model)
        {
            var cancelBets = new CancelBetHistoryModel();
            var request = new ActivityLogRequest
            {
                Page = Page.Admin_CancelBets.ToString().Replace("_", "."),
                Payload = $"",
                MetaData = MetaData
            };

            var response = await BackOfficeService.GetActivityLogs(request);

            if (response.StatusCode == StatusCode.Success)
            {
                MetaData = response.Result.MetaData;

                var temp = response.Result.ActivityLogs;

                foreach (var item in temp)
                {
                    var betDetails = JsonSerializer.Deserialize<CancelBetHistoryModel.BetDetails>(item.From);
                    if(betDetails != null)
                    {
                        var betSelectionDetails = JsonSerializer.Deserialize<CancelBetHistoryModel.BetSelectionDetails>(item.To);
                        betDetails.Selections = betSelectionDetails ?? new CancelBetHistoryModel.BetSelectionDetails();

                        cancelBets.CancelBets.Add(betDetails);
                    }

                }

            }

            return cancelBets;
        }

        #endregion
    }
}
