using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using RabbitMQ.Client.Apigen.Attributes;
using System.Text;
using System.Text.Json;
using static Backoffice.Shared.Models.ManualBetDetailsModel;

namespace Backoffice.Web.Pages.Administration
{
    public partial class ManualBetSettlement : IDisposable
    {
        #region Properties
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [Inject] private IMessageBroker MessageBroker { get; set; }
        [Inject] private ToastService? _toasterService { get; set; }

        public List<ActivityLogModel> ActivityLogs { get; set; }
        public List<AlertModel> Alerts { get; set; }

        public MetaData MetaData { get; set; }
        public ManualBetSettlementCallbackModel CallbackModel { get; set; }
        
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        private int CurrentPage { get; set; } = 1;
        private bool IsPreloader { get; set; }
        #endregion

        #region Fields
        private ToastService toasterService => _toasterService!;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Manual Bet Settlement";
            IconClass = "ico ico-manual-bet-settlement";

            Alerts = new List<AlertModel>();
            IsPreloader = true;
            //StateHasChanged();

            CallbackModel = new ManualBetSettlementCallbackModel();

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 0,
                TotalPages = 1,
            };

            await GetContents();
        }        

        public async Task OnSearchButtonClicked()
        {
            IsPreloader = true;
            StateHasChanged();
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
                    Status = response.StatusCode.ToString(),
                    Message = "BetId's have been successfully settled",
                    Opacity = 1.0m
                });
            }
            else
            {
                Alerts.Add(new AlertModel()
                {
                    Status = response.StatusCode.ToString(),
                    Message = response.Message,
                    Opacity = 1.0m
                });
            }                    

            await GetContents();
        }

        public async Task OnExportExcel()
        {
            var payload = new OnGetManualBetSettlementHistoryMessage()
            {
                ActivityLogs = ActivityLogs
            };

            MessageBroker.Publish(JsonSerializer.Serialize(payload), "ExportManualBetSettlementHistory", "r.bo.util.export", Guid.NewGuid().ToString());

            toasterService.AddToast(Toast.NewToast
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
        }

        #endregion

        #region Utilities
        private async Task OnInitEvent(ManualBetSettlementCallbackModel callbackModel)
        {
            CallbackModel = callbackModel;                        
        }


        private async Task GetContents()
        {
            if(IsPreloader == false)
            {
                IsPreloader = true;
                StateHasChanged();
            }

            var response = await BackOfficeService.GetManualBetSettlementActivityLogs(new ManualBetSettlementActivityLogRequest()
            {
                ManualBetSettlementCallbackModel = CallbackModel,
                MetaData = MetaData
            });

            ActivityLogs = response.Result.ActivityLogs;
            MetaData = response.Result.MetaData;
            #region old get activitylog
            //try
            //{
            //    var response = (await BackOfficeService.GetActivityLogs(new ActivityLogRequest()
            //    {
            //        Page = Page.Admin_Settlement_Bet.ToString().Replace('_', '.'),
            //        DateFrom = CallbackModel != null ? CallbackModel.DateFrom.Add(new TimeSpan(0, 0, 0)) : DateTime.UtcNow.AddDays(-1).Date.Add(new TimeSpan(0, 0, 0)),
            //        DateTo = CallbackModel != null ? CallbackModel.DateTo.Add(new TimeSpan(23, 59, 59)) : DateTime.UtcNow.Date.Add(new TimeSpan(23, 59, 59)),
            //        MetaData = MetaData
            //    }));

            //    if (response.StatusCode == StatusCode.Success)
            //    {
            //        if (response.Result != null && response.Result.ActivityLogs.Any())
            //        {

            //            if (CallbackModel != null)
            //            {
            //                FilterLogs(response);
            //            }

            //            ActivityLogs = response.Result.ActivityLogs;
            //            MetaData = response.Result.MetaData;
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{

            //}
            #endregion

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
        #endregion
    }
}
