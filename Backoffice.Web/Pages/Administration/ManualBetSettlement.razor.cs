using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text;
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

        public async Task OnExportExcel()
        {
            var payload = new OnGetManualBetSettlementHistoryMessage()
            {
                ManualBetSettlementCallbackModel = CallbackModel,
                ConnectionId = _userState.ConnectionId
            };

            // Task - Migrate to api call
            //MessageBroker.Publish(JsonSerializer.Serialize(payload), "ExportManualBetSettlementHistory", "r.bo.util.export", Guid.NewGuid().ToString());

            _toasterService.AddToast(Toast.NewToast
                    ("Manual Bet Settlement",
                    $"Success! Excel file is being generated system will notify once done."));
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

            if (response.Result.ActivityLogs.Any())
            {
                IsExportDisabled = false;
            }

            ActivityLogs = response.Result.ActivityLogs.ToList();
            MetaData = response.Result.MetaData;

            IsPreloader = false;

        }

        private string BuildEventDetails(BetSelectionDetails betSelectionDetails, string oddsFormat)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append($"<span class=\"segment-league d-block\">{betSelectionDetails.LeagueName}</span>");
            builder.Append($"<span class=\"fixture d-block\">{betSelectionDetails.FixtureName}</span>");
            builder.Append($"<span class=\"start-date d-block\">{betSelectionDetails.FixtureStartDate.ToString("MM/dd/yyyy hh:mm:ss tt").ToUpper()}</span>");
            builder.Append($"<span class=\"selection-details d-block\">{betSelectionDetails.SelectionName}</span>");
            builder.Append($"<span class=\"market-name d-block\">{betSelectionDetails.MarketName} @ {betSelectionDetails.Odds} ({oddsFormat})</span>");
            builder.Append($"<span class=\"score d-block\">{betSelectionDetails.BetScore}, Final Score: {betSelectionDetails.SettlementScore}</span>");


            return builder.ToString();
        }

        public string GetBetStatusName(ManualBetDetails betDetails)
        {
            if (string.IsNullOrEmpty(betDetails.NewBetStatusName))
            {
                return ((BetStatus)(int.Parse(betDetails.NewBetStatusId))).ToString();
            }
            else
            {
                return betDetails.NewBetStatusName;
            }
        }
        #endregion
    }
}
