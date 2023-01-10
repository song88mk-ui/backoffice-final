using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.MarketGroups
{
    public partial class ViewMarketGroupModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }

        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public string MarketTemplateGroupName { get; set; }
        public List<ClientMarketTemplateModel> ClientMarketTemplates { get; set; }
        protected bool IsPreLoader { get; set; } = false;
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);

            ClientCode = ModalParameters.TryGet<string>("ClientCode");
            SportId = ModalParameters.TryGet<int>("SportId");
            FixtureStatusId = ModalParameters.TryGet<int>("FixtureStatusId");
            MarketTemplateGroupName = ModalParameters.TryGet<string>("MarketTemplateGroupName");

            var result = (await TradingService.GetClientMarketTemplates(ClientCode, SportId, FixtureStatusId)).Result;

            ClientMarketTemplates = result != null ? result.MarketTemplates : new List<ClientMarketTemplateModel>();

            ClientMarketTemplates = ClientMarketTemplates.Where(c => MarketTemplateGroupName.ToLower() == "all markets" || c.MarketTemplateGroupNames.Any(m => m.ToLower() == MarketTemplateGroupName.ToLower())).ToList();

            IsPreLoader = false;
            await InvokeAsync(StateHasChanged);

        }
        public async Task OnModalCancel()
        {
            ModalService.Cancel();
        }
        public void Dispose()
        {

        }
        #endregion
    }
}
