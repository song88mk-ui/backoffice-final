using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.TopLeagues
{
    public partial class EditModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        public GetAllLeaguesRequest GetAllLeaguesRequest { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            var clientId = ModalParameters.TryGet<int>("ClientId");
            var sportId = ModalParameters.TryGet<int>("SportId");
            var topLeagues = ModalParameters.TryGet<int>("CurrentTopLeagues");


            GetAllLeaguesRequest = new GetAllLeaguesRequest()
            {
                ClientIdRef = clientId,
                SportId = sportId,
                LanguageCode = "en"
            };

            var allleagues = BackOfficeService.GetAllLeaguesAsync(GetAllLeaguesRequest);

            await InvokeAsync(StateHasChanged);
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
