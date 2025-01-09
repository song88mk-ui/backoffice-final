using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Drawing;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class CopyModal : IDisposable
    {
        #region Properties

        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        public List<ClientInformationModel> ClientInformations { get; set; }
        public List<SelectListItemModel<string>> Clients { get; set; }
        public string SelectedClientCode { get; set; }

        public string FromClientCode { get; set; }
        private string Region { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Clients = new List<SelectListItemModel<string>>();
            Region = ModalParameters.TryGet<string>("Region").ToUpper();
            TradingService.Region = Region;

            FromClientCode = ModalParameters.TryGet<string>("FromClientCode");

            var clientResponse = await TradingService.GetClientList();

            if (clientResponse.StatusCode == StatusCode.Success)
            {
                clientResponse.Result = _userState.ClientCodes.Any() ? clientResponse.Result.Where(c => _userState.ClientCodes.Contains(c.ClientCode)).ToList() : new List<ClientInformationModel>();

                ClientInformations = clientResponse.Result;

                clientResponse.Result = ClientInformations.Where(c => c.ClientCode != FromClientCode).ToList();

                Clients = clientResponse.Result.Select(s => new SelectListItemModel<string>
                {
                    Value = s.ClientCode,
                    Name = s.ClientName
                }).ToList();
            }
            else
            {
                Clients.Add(new SelectListItemModel<string>
                {
                    Value = "",
                    Name = _translationState.GetValue("No Clients Available")
                });
            }
        }

        public async Task OnClientChangedEvent(ChangeEventArgs e)
        {
            SelectedClientCode = e.Value.ToString();
        }

        protected async Task OnCopyClickEvent()
        {
            if (string.IsNullOrEmpty(SelectedClientCode))
                return;

            var parameters = new ModalParameters();

            var toClient = ClientInformations.FirstOrDefault(c => c.ClientCode == SelectedClientCode);

            parameters.Add("ToClientCode", toClient.ClientCode);
            parameters.Add("ToClientName", toClient.ClientName);
            parameters.Add("Sports", ModalParameters.TryGet<List<SportsSortOrder>>("Sports"));
            parameters.Add("Region", Region);

            parameters.Add("DialogStyle", "max-width: 600px;");

            var options = new ModalOptions();
            ModalService.Open<CopyConfirmationModal>("Copy Setting", parameters, options);
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
