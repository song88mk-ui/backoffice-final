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
        [Inject] public ToastService ToastService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        public string FromClientCode { get; set; }
        public int FromSportId { get; set; }
        public int FromFixtureStatusId { get; set; }

        public List<ClientInformationModel> ClientInformations { get; set; }
        public List<SelectListItemModel<string>> Clients { get; set; }
        public string SelectedClientCode { get; set; }

        public List<Backoffice.Web.Models.SportModel> Sports { get; set; }
        public int SelectedSportId { get; set; }

        public List<SelectListItemModel<int>> Scopes { get; set; }
        public int SelectedScopeId { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ClientInformations = new List<ClientInformationModel>();
            Clients = new List<SelectListItemModel<string>>();
            Sports = new List<Backoffice.Web.Models.SportModel>();
            Scopes = new List<SelectListItemModel<int>>();

            FromClientCode = ModalParameters.TryGet<string>("FromClientCode");
            FromSportId = ModalParameters.TryGet<int>("FromSportId");
            FromFixtureStatusId = ModalParameters.TryGet<int>("FromFixtureStatusId");

            // Load clients
            if (_userState.Model.TradingClients != null && _userState.Model.TradingClients.Any())
            {
                ClientInformations = _userState.Model.TradingClients.Select(c => new ClientInformationModel
                {
                    ClientCode = c.ClientCode,
                    ClientId = c.ClientId,
                    ClientName = c.ClientName,
                }).ToList();
                Clients = _userState.Model.TradingClients.Select(s => new SelectListItemModel<string>
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

            // Load sports from the same lookup used by the MarketSortOrder filter so both lists match.
            try
            {
                var sportsResponse = await _backofficeService.GetSportLookup(_translationState.ActiveLanguageCode.ToString());
                if (sportsResponse?.Result?.SportLookup != null)
                {
                    Sports = sportsResponse.Result.SportLookup.Select(s => new Backoffice.Web.Models.SportModel
                    {
                        SportId = s.SportId,
                        SportName = s.SportName
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading sports: {ex.Message}");
            }

            // Load scopes
            Scopes = Enum.GetValues(typeof(Scope))
                .Cast<Scope>()
                .Select(s => new SelectListItemModel<int>
                {
                    Value = (int)s,
                    Name = s.ToString()
                })
                .ToList();
        }

        public async Task OnClientChangedEvent(ChangeEventArgs e)
        {
            SelectedClientCode = e.Value.ToString();
        }

        public async Task OnSportChangedEvent(ChangeEventArgs e)
        {
            SelectedSportId = Convert.ToInt32(e.Value);
        }

        public async Task OnScopeChangedEvent(ChangeEventArgs e)
        {
            SelectedScopeId = Convert.ToInt32(e.Value);
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnCopyClickEvent()
        {
            if (string.IsNullOrEmpty(SelectedClientCode) || SelectedSportId <= 0 || SelectedScopeId < 0)
            {
                ToastService.AddToast(Toast.NewToast(
                    _translationState.GetValue("Copy Market Sort Order"),
                    _translationState.GetValue("Please select the client, sport, and scope you want to copy to.")));
                return;
            }

            if (FromClientCode == SelectedClientCode && SelectedSportId == FromSportId && SelectedScopeId == FromFixtureStatusId)
            {
                ToastService.AddToast(Toast.NewToast(
                    _translationState.GetValue("Copy Market Sort Order"),
                    _translationState.GetValue("You're copying to the same client, sport, and scope. Change at least one before copying.")));
                return;
            }

            var fromClient = ClientInformations.FirstOrDefault(c => c.ClientCode == FromClientCode);
            var toClient = ClientInformations.FirstOrDefault(c => c.ClientCode == SelectedClientCode);

            var toSportName = Sports.FirstOrDefault(s => s.SportId == SelectedSportId)?.SportName;
            var toScopeName = Scopes.FirstOrDefault(s => s.Value == SelectedScopeId)?.Name;

            var parameters = new ModalParameters();

            parameters.Add("ClientName", toClient.ClientName);
            parameters.Add("SportName", toSportName);
            parameters.Add("FixtureStatusName", toScopeName);

            parameters.Add("FromClientId", fromClient.ClientId);
            parameters.Add("ToClientId", toClient.ClientId);

            parameters.Add("SportId", FromSportId);
            parameters.Add("FixtureStatusId", FromFixtureStatusId);

            parameters.Add("SelectedSportId", SelectedSportId);
            parameters.Add("SelectedScopeId", SelectedScopeId);

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
