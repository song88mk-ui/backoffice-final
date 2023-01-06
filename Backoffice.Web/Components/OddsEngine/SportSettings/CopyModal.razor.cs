using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class CopyModal : IDisposable
    {
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        public ClientSportOrderFilterModel ClientSportOrderFilter { get; set; }
        public bool IsPreLoader { get; set; }
        public string CopyClientName { get; set; }
        public string CopyClientCode { get; set; }
        public string CopyToClientCode { get; set; }
        public string CopyToClientName { get; set; }


        protected override async Task OnInitializedAsync()
        {
            IsPreLoader = false;
            ClientSportOrderFilter = ModalParameters.TryGet<ClientSportOrderFilterModel>("ClientSportOrderFilter");

            CopyClientCode = ClientSportOrderFilter.ClientCode;
            CopyClientName = ClientSportOrderFilter.ClientName;
            CopyToClientCode = string.Empty;
            CopyToClientName = string.Empty;
        }
        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnClientChangedEvent(ChangeEventArgs e)
        {
            CopyToClientCode = e.Value.ToString();
            CopyToClientName = string.Empty;
            if (!string.IsNullOrEmpty(CopyToClientCode))
            {
                var client = ClientSportOrderFilter?.Clients?.FirstOrDefault(c => c.Value == CopyToClientCode);
                if (client != null)
                    CopyToClientName = client.Name;
            }
        }
        protected async Task OnCopyConfirmation()
        {
            if (string.IsNullOrEmpty(CopyToClientCode))
                return;

            var parameters = new ModalParameters();
            parameters.Add("CopyClientCode", CopyClientCode);
            parameters.Add("CopyClientName", CopyClientName);
            parameters.Add("CopyToClientCode", CopyToClientCode);
            parameters.Add("CopyToClientName", CopyToClientName);
            parameters.Add("ClientSportOrderList", ModalParameters.TryGet<List<SportsSortOrder>>("ClientSportOrderList"));

            parameters.Add("DialogStyle", "max-width: 600px;");

            var options = new ModalOptions();
            //ModalService.Open<CopyConfirmationModalBody>("Copy Setting", parameters, options);
        }

        public void Dispose()
        {
        }
    }
}
