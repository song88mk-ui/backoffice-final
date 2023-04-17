using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class CopyModal : IDisposable
    {
        #region Properties

        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        public List<ClientInformationModel> ClientInformations { get; set; }
        public List<SelectListItemModel<string>> Clients { get; set; }
        public string SelectedClientCode { get; set; }

        public string FromClientCode { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Clients = new List<SelectListItemModel<string>>();
            TradingService.Region = ModalParameters.TryGet<string>("Region").ToUpper();

            FromClientCode = ModalParameters.TryGet<string>("FromClientCode");

            var clientResponse = await TradingService.GetClientList();

            if (clientResponse.StatusCode == StatusCode.Success)
            {
                if (UserModel.ClientCodes.Any())
                    clientResponse.Result = clientResponse.Result.Where(c => UserModel.ClientCodes.Contains(c.ClientCode)).ToList();

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
                    Name = "No Clients Available"
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
