using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Mapping;
using Backoffice.Web.Services;
using Backoffice.Web.Components.Common;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class EditMappingModal: IDisposable
    {
        [Inject] public IModalService ModalService { get; set; }
        private UnmappedModel _draggedProviderItem;
        private List<UnmappedModel> _unmappedSports;
        private string _formId = "editMappingFormId";
        
        protected override async Task OnInitializedAsync()
        {
            var response = await _providerMappingService.GetUnmappedSports();
            _unmappedSports = response.Result.Unmapped;
        }

        private void HandleDragProviderItem(UnmappedModel providerItem)
        {
            _draggedProviderItem = providerItem;
        }

        private void HandleUpdateMappingClick(MappingSetupItemModel item)
        {
            
        }

        public void OnCloseClickEvent()
        {
            ModalService.Cancel();
        }

        public void OnSaveClickEvent()
        {
            ModalService.Cancel();
        }
        public void Dispose()
        {
        }
    }
}
