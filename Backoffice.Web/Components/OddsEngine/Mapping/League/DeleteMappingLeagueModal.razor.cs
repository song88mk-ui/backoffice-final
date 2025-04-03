using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Mapping;
using Backoffice.Web.Services;
using Backoffice.Web.Components.Common;
using Microsoft.AspNetCore.Components;


namespace Backoffice.Web.Components.OddsEngine.Mapping.League
{
    public partial class DeleteMappingLeagueModal: IDisposable
    {
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter] private CustomModal Modal { get; set; }

        private string _leagueName;

        protected override async Task OnInitializedAsync()
        {
            _leagueName = ModalParameters.TryGet<string>("LeNa");
        }
        public void OnCloseClickEvent()
        {
            ModalService.Cancel();
        }
        public void OnDeleteClickEvent()
        {
            ModalService.Cancel();
        }
        public void Dispose()
        {
        }
    }

}