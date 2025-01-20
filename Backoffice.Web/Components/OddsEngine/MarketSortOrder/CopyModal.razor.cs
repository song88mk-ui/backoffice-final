using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.MarketSortOrder
{
    public partial class CopyModal : IDisposable
    {
        #region Properties

        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        public string FromClientCode { get; set; }
        public int FromSportId { get; set; }
        public int FromFixtureStatusId { get; set; }

        public List<ClientInformationModel> ClientInformations { get; set; }
        public List<SelectListItemModel<string>> Clients { get; set; }
        public string SelectedClientCode { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ClientInformations = new List<ClientInformationModel>();
            Clients = new List<SelectListItemModel<string>>();

            FromClientCode = ModalParameters.TryGet<string>("FromClientCode");
            FromSportId = ModalParameters.TryGet<int>("FromSportId");
            FromFixtureStatusId = ModalParameters.TryGet<int>("FromFixtureStatusId");

            var clientResponse = await TradingService.GetClientList();

            if (clientResponse.StatusCode == StatusCode.Success)
            {
                clientResponse.Result = _userState.Model.ClientCodes.Any() ? clientResponse.Result.Where(c => _userState.Model.ClientCodes.Contains(c.ClientCode)).ToList() : new List<ClientInformationModel>();

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

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnCopyClickEvent()
        {
            if (string.IsNullOrEmpty(SelectedClientCode) || FromClientCode == SelectedClientCode)
                return;

            var fromClient = ClientInformations.FirstOrDefault(c => c.ClientCode == FromClientCode);
            var toClient = ClientInformations.FirstOrDefault(c => c.ClientCode == SelectedClientCode);

            var parameters = new ModalParameters();

            parameters.Add("ClientName", toClient.ClientName);
            parameters.Add("SportName", ModalParameters.TryGet<string>("FromSportName"));
            parameters.Add("FixtureStatusName", ModalParameters.TryGet<string>("FromFixtureStatusName"));

            parameters.Add("FromClientId", fromClient.ClientId);
            parameters.Add("ToClientId", toClient.ClientId);

            parameters.Add("SportId", FromSportId);
            parameters.Add("FixtureStatusId", FromFixtureStatusId);

            parameters.Add("UserId", _userState.Model.Id);

            parameters.Add("DialogStyle", "max-width: 600px;");

            var options = new ModalOptions();
            ModalService.Open<CopyConfirmationModal>("Copy Market Sort Order", parameters, options);
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
