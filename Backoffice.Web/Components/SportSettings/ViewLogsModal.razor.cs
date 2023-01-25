using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.SportSettings
{
    public partial class ViewLogsModal : IDisposable
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
            ClientSportOrderAudit = new List<ClientSportOrderAuditResponse>();

            IsPreLoader = true;

            ClientSportOrderAudit = (await TradingService.GetClientSportOrderAuditList(new ClientSportOrderAuditRequest()
            {
                ClientCode = ModalParameters.TryGet<string>("ClientCode")
            })).Result;

            IsPreLoader = false;
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
