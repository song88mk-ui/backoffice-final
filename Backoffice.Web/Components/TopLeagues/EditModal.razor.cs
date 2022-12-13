using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Requests.GetTopLeaguesRequest;
using static Backoffice.Shared.Models.Response.GetTopLeaguesResponse;

namespace Backoffice.Web.Components.TopLeagues
{
    public partial class EditModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        public GetAllLeaguesRequest GetAllLeaguesRequest { get; set; }
        public GetAllLeaguesResponse GetAllLeaguesResponse { get; set;}
        [Parameter] public int ClientId { get; set; }

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            ClientId = ModalParameters.TryGet<int>("ClientId");
            var sportId = ModalParameters.TryGet<string>("SportId");
            var topLeagues = ModalParameters.TryGet<List<OrderedClientTopLeague>>("CurrentTopLeagues");


            GetAllLeaguesRequest = new GetAllLeaguesRequest()
            {
                ClientIdRef = ClientId,
                SportId = string.IsNullOrWhiteSpace(sportId) ? 0 : int.Parse(sportId),
                SegmentId = 0,
                LeagueId = 0,
                LanguageCode = "en"
            };

            await OnInitCallback();
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnInitCallback()
        {
            GetAllLeaguesResponse = (await BackOfficeService.GetAllLeaguesAsync(GetAllLeaguesRequest)).Result;
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnSaveClickEvent()
        {

        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
