using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using ActivityLogModel = Backoffice.Shared.Models.ActivityLogModel;

namespace Backoffice.Web.Components.OddsEngine.MarketGroups
{
    public partial class MarketGroupViewLogsModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
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
            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 0,
                TotalPages = 0,
            };

            MarketGroupModel = ModalParameters.TryGet<MarketGroupModel>("MarketGroupModel");

            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);
            await GetContent();
        }

        public async Task OnCloseModalClickEvent()
        {
            ModalService.Cancel();
        }
        protected async Task SelectedPage(int page)
        {
            MetaData.CurrentPage = page;
            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);
            await GetContent();
        }
        public void Dispose()
        {
            
        }
        #endregion

        #region Utilities
        private async Task GetContent()
        {
            var response = (await BackOfficeService.GetActivityLogs(new ActivityLogRequest
            {
                Page = Page.OE_Administration_MarketGroup.ToString().Replace("_", "."),
                Payload = $"{MarketGroupModel.ClientCode}.{MarketGroupModel.SportId}.{MarketGroupModel.FixtureStatusId}",
                MetaData = MetaData
            })).Result;

            if (response != null)
            {
                ActivityLogs = response.ActivityLogs;
                MetaData = response.MetaData;
            }
            IsPreLoader = false;
            await InvokeAsync(StateHasChanged);
        }
        #endregion
    }
}
