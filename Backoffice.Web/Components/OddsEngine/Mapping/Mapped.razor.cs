using Backoffice.Web.Models;
using System.Drawing;
using Microsoft.AspNetCore.Components;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.OddsEngine.Mapping;
using Backoffice.Web.Services;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class Mapped
    {
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        private List<SportMappingModel> _sportMapping = new();
        protected override async Task OnInitializedAsync()
        {
            var response = await _providerMappingService.GetSportMapping();
            _sportMapping = response.Result?.SportMapping ?? new List<SportMappingModel>();
        }
        public async Task OnEditMappingClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-lg");
            
            var options = new ModalOptions();

            ModalService.Open<EditMappingModal>("Edit Mapping Modal", parameters, options);
        }

        public async Task OnSaveClickEvent()
        {
        }
    }
}
