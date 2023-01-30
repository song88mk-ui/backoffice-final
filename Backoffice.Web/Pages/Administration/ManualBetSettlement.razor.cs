using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
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
        
        public MetaData MetaData { get; set; }        
        public List<ActivityLogModel> ActivityLogs { get; set; }
        public ManualBetSettlementCallbackModel CallbackModel { get; set; }
        

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        private int CurrentPage { get; set; } = 1;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Manual Bet Settlement";
            IconClass = "ico ico-manual-bet-settlement";            

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 100,
                TotalCount = 0,
                TotalPages = 1,
            };

            //await GetContents();
        }        

        private async Task OnSearchButtonClicked()
        {
            await GetContents();
        }

        private async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;
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
            var response = (await BackOfficeService.GetActivityLogs(new ActivityLogRequest()
            {
                Page = Page.Admin_Settlement_Bet.ToString().Replace('_', '.'),                
                MetaData = MetaData
            }));

            if (response.StatusCode == StatusCode.Success)
            {
                if (response.Result != null && response.Result.ActivityLogs.Any())
                {

                }
            }
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
