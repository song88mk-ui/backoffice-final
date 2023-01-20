using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.SportSettings
{
    public partial class SportSettingsCopyModal : IDisposable
    {
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        public ClientSportOrderFilterModel ClientSportOrderFilter { get; set; }
        public bool IsPreLoader { get; set; }
        public string CopyFromClientName { get; set; }
        public string CopyFromClientCode { get; set; }
        public string CopyToClientCode { get; set; }
        public string CopyToClientName { get; set; }


        protected override async Task OnInitializedAsync()
        {
            IsPreLoader = false;
            ClientSportOrderFilter = ModalParameters?.TryGet<ClientSportOrderFilterModel>("ClientSportOrderFilter");
            if (ClientSportOrderFilter != null)
            {
                CopyToClientCode = ClientSportOrderFilter.ClientCode;
                CopyToClientName = ClientSportOrderFilter.ClientName;
                CopyFromClientCode = string.Empty;
                CopyFromClientName = string.Empty;
            }
        }
        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnClientChangedEvent(ChangeEventArgs e)
        {
            CopyFromClientCode = e.Value.ToString();
            CopyFromClientName = string.Empty;
            if (!string.IsNullOrEmpty(CopyFromClientCode))
            {
                var client = ClientSportOrderFilter?.Clients?.FirstOrDefault(c => c.Value == CopyFromClientCode);
                if (client != null)
                    CopyFromClientName = client.Name;
            }
        }
        protected async Task OnCopyConfirmation()
        {
            if (string.IsNullOrEmpty(CopyToClientCode))
                return;

            var parameters = new ModalParameters();
            parameters.Add("CopyClientCode", CopyFromClientCode);
            parameters.Add("CopyClientName", CopyFromClientName);
            parameters.Add("CopyToClientCode", CopyToClientCode);
            parameters.Add("CopyToClientName", CopyToClientName);

            parameters.Add("DialogStyle", "max-width: 600px;");

            var options = new ModalOptions();
            ModalService.Open<CopyConfirmationModalBody>("Copy Setting", parameters, options);
        }

        public void Dispose()
        {
        }
    }
}
