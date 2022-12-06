using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.TopLeagues;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages
{
    public partial class TopLeagues : IDisposable
    {
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter(Name = "UserModel")] protected UserModel UserModel { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public string ClientName { get; set; } = "";
        public GetTopLeaguesRequest GetTopLeaguesRequest { get; set; }
        public GetTopLeaguesResponse TopLeaguesList { get; set; }
        public ClientListResponse ClientInformation { get; set; }
        public MetaData MetaData { get; set; } = new MetaData();

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Top Leagues";
            IconClass = "ico-top-leagues";

            ClientInformation = (await BackOfficeService.GetClients()).Result;
            var userClientCodes = ClientInformation != null ? ClientInformation?.Clients.Where(x => UserModel.ClientCodes.Contains(x.ClientCode)).Select(x => x.ClientId).ToList() : new List<long>();

            GetTopLeaguesRequest = new GetTopLeaguesRequest()
            {
                ClientIdRefs = string.Join(",", userClientCodes),
                SportId = 0,
                LanguageCode = "en"
            };

            TopLeaguesList = (await BackOfficeService.GetTopLeaguesAsync(GetTopLeaguesRequest)).Result;
            if (TopLeaguesList != null)
                TopLeaguesList.ClientTopLeagues.ToList().ForEach(c => c.ClientName = ClientInformation.Clients.FirstOrDefault(x => x.ClientId == c.ClientIdRef).ClientName);
        }



        public async Task OnEdit()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");

            var options = new ModalOptions();
            ModalService.Open<EditModal>($"Set Top Leagues of {ClientName}", parameters, options);
        }

        public async Task OnCopy()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");

            var options = new ModalOptions();
            ModalService.Open<CopyModal>($"Copy {ClientName} Top League Settings To", parameters, options);
        }

        public void Dispose()
        {
        }
    }
}
