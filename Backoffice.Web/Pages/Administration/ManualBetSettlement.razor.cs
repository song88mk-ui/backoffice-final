using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using RabbitMQ.Client.Apigen.Attributes;

namespace Backoffice.Web.Pages.Administration
{
    public partial class ManualBetSettlement : IDisposable
    {
        #region Properties
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public MetaData MetaData { get; set; }
        public ManualBetSettlementFilterModel FilterModel { get; set; }
        public ManualSettlementBetHistoryModel ManualSettlementBetHistoryModel { get; set; }
        private int CurrentPage { get; set; } = 1;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Manual Bet Settlement";
            IconClass = "ico ico-manual-bet-settlement";

            FilterModel = new ManualBetSettlementFilterModel();

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 100,
                TotalCount = 0,
                TotalPages = 1,
            };

            await GetContents();
        }

        public async Task GetContents()
        {
            //var response = (await BackOfficeService.GetManualBetSettlmentHistory(new ManualBetDetailHistoryRequest()
            //{
            //    Page = MetaData.CurrentPage,
            //    PageSize = MetaData.PageSize,
            //    ClientIdRefs = string.Join(',', FilterModel.Clients.Select(x => x.Value)),
            //    SportIds = string.Join(',', FilterModel.Sports.Select(x => x.Value)),
            //    Currencies = string.Join(',', FilterModel.Currencies.Select(x => x.Value)),
            //    FromStatus = string.Join(',', FilterModel.FromStatuses.Select(x => x.Value)),
            //    ToStatus = string.Join(',', FilterModel.ToStatuses.Select(x => x.Value)),
            //    ReferenceIds = FilterModel.BetId,
            //    FromSettleDate = FilterModel.DateFrom,
            //    ToSettleDate = FilterModel.DateTo,
            //    MemberName = FilterModel.MemberName,
            //    SettledBy = FilterModel.SettledBy
            //})).Result;

            //if (response != null)
            //{

            //}

            //var response = (await BackOfficeService.GetActivityLogs(new ActivityLogRequest()
            //{
            //    Page = Page.Administration_Settlement_Bet.ToString().Replace('_','.'),
            //    Payload = BuildPayload(),
            //    MetaData = MetaData
            //}));
        }

        public async Task OnSearchButtonClicked()
        {
            await GetContents();
        }

        public async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities
        private string BuildPayload()
        {
            return "";
        }
        #endregion
    }
}
