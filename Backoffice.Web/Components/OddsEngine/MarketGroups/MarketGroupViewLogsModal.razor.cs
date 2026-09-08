using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using ActivityLogModel = Backoffice.Shared.Models.ActivityLogModel;

namespace Backoffice.Web.Components.OddsEngine.MarketGroups
{
    public partial class MarketGroupViewLogsModal : IDisposable
    {
        #region Properties

        [CascadingParameter] public ModalParameters ModalParameters { get; set; }

        public MarketGroupModel MarketGroupModel { get; set; }
        public List<ActivityLogModel> ActivityLogs { get; set; }
        public MetaData MetaData { get; set; } = new MetaData();
        protected bool IsPreLoader { get; set; } = false;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ActivityLogs = null;

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 0,
                TotalPages = 0,
            };

            _tradingService.Region = ModalParameters.TryGet<string>("Region").ToUpper();
            MarketGroupModel = ModalParameters.TryGet<MarketGroupModel>("MarketGroupModel");

            await GetContent();
        }

        public async Task OnCloseModalClickEvent()
        {
            _modalService.Cancel();
        }

        protected async Task SelectedPage(int page)
        {
            MetaData.CurrentPage = page;

            await GetContent();
        }

        public void Dispose()
        {

        }

        #endregion

        #region Utilities

        private async Task GetContent()
        {
            IsPreLoader = true;

            var response = (await _backofficeService.GetActivityLogs(new ActivityLogRequest
            {
                Page = SportsbookClientPermission.SBC_OE_MarketSetting_Group.ToString().Replace("_", "."),
                Payload = $"{MarketGroupModel.ClientCode}.{MarketGroupModel.SportId}.{MarketGroupModel.FixtureStatusId}",
                MetaData = MetaData,
                DateFrom = DateTimeHelper.Now(_userState.TimeZoneOffset).Date.AddYears(-1)
            })).Result;

            if (response != null)
            {
                ActivityLogs = response.ActivityLogs;
                MetaData = response.MetaData;
            }

            IsPreLoader = false;
        }

        #endregion
    }
}
