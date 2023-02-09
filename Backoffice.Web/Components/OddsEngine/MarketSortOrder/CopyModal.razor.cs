using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
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
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        public string FromClientCode { get; set; }
        public int FromSportId { get; set; }
        public int FromFixtureStatusId { get; set; }

        public List<ClientInformationModel> ClientInformations { get; set; }
        public List<SelectListItemModel<string>> Clients { get; set; }
        public string SelectedClientCode { get; set; }
        //public List<SelectListItemModel<int>> Sports { get; set; }
        //public int SelectedSportId { get; set; }
        //public List<SelectListItemModel<int>> FixtureStatuses { get; set; }
        //public int SelectedFixtureStatusId { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ClientInformations = new List<ClientInformationModel>();
            Clients = new List<SelectListItemModel<string>>();
            //Sports = new List<SelectListItemModel<int>>();
            //FixtureStatuses = new List<SelectListItemModel<int>>();

            FromClientCode = ModalParameters.TryGet<string>("FromClientCode");
            FromSportId = ModalParameters.TryGet<int>("FromSportId");
            FromFixtureStatusId = ModalParameters.TryGet<int>("FromFixtureStatusId");

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

            //var sports = (await TradingService.GetSportLookup()).Result;

            //if (sports != null)
            //{
            //    Sports = sports.SportLookup.Select(s => new SelectListItemModel<int>
            //    {
            //        Value = s.SportId,
            //        Name = s.SportName
            //    }).ToList();
            //}
            //else
            //{
            //    Sports.Add(new SelectListItemModel<int>
            //    {
            //        Value = 0,
            //        Name = "No Sports Available"
            //    });
            //}

            //FixtureStatuses.Add(new SelectListItemModel<int>
            //{
            //    Value = (int)FixtureStatus.Prematch,
            //    Name = FixtureStatus.Prematch.ToString()
            //});

            //FixtureStatuses.Add(new SelectListItemModel<int>
            //{
            //    Value = (int)FixtureStatus.Live,
            //    Name = FixtureStatus.Live.ToString()
            //});

            //SelectedFixtureStatusId = -1;
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

            parameters.Add("UserId", UserModel.Id);

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
