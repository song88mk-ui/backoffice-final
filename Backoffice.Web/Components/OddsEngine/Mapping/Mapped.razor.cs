using Backoffice.Web.Models;
using System.Drawing;
using Microsoft.AspNetCore.Components;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Models.Requests;
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
            await FetchSportMapping();
        }
        
        private async Task FetchSportMapping()
        {
            var request = new MappingRequest
            {
                Page = 1,
                PageSize = 1000
            };

            var response = await _providerMappingService.GetSportMapping(request);
            _sportMapping = response.Result.SportMapping.OrderBy(sm => sm.SpNa).ToList();
        }
        public async Task HandleEditMappingClickEvent(SportMappingModel mappingItem)
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("SpId", mappingItem.SpId);
            var options = new ModalOptions
            {
                HideHeader = true
            };

            ModalService.Open<EditMappingModal>("Edit Mapping", parameters, options);
        }

        public async Task OnSaveClickEvent()
        {
        }
    }
}