using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class ViewLogsModalBody : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        public List<ClientSportOrderAuditResponse> ClientSportOrderAudit { get; set; }
        protected bool IsPreLoader { get; set; } = false;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ClientSportOrderAudit = null;

            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);

            await GetContent();
        }

        private async Task GetContent()
        {
            var clientCode = ModalParameters.TryGet<string>("ClientCode");

            ClientSportOrderAudit = (await TradingService.GetClientSportOrderAuditList(new ClientSportOrderAuditRequest()
            {
                ClientCode = clientCode
            })).Result;

            IsPreLoader = false;
            await InvokeAsync(StateHasChanged);
        }
        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
