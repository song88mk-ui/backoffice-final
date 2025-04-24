using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Mapping;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Mapping.Sport
{
    public partial class MappedSport
    {
        [Inject] public IModalService ModalService { get; set; }

        [Parameter] public EventCallback<UpdateMappingTabCountModel> OnItemsCountUpdateCallback { get; set; }

        private string _searchQuery = string.Empty;
        private List<SportMappingModel> _sportMapping = new List<SportMappingModel>();
        private List<SportMappingModel> _filteredSportMapping = new List<SportMappingModel>();
        private UpdateMappingTabCountModel _countCallbackModel { get; set; }
        private bool _shouldRender;

        protected override bool ShouldRender() => _shouldRender;
        protected override async Task OnInitializedAsync()
        {
            await FetchSportMapping();

            _shouldRender = true;
        }

        private async Task FetchSportMapping()
        {
            var request = new MappingRequest
            {
                Page = 1,
                PageSize = 1000,
            };

            var response = await _providerMappingService.GetSportMapping(request);
            _sportMapping = response.Result.SportMapping.OrderBy(sm => sm.SpNa).ToList();
            _filteredSportMapping = _sportMapping;

            _shouldRender = true;
        }

        private void PerformSearch(ChangeEventArgs e)
        {
            _searchQuery = e.Value.ToString();
            _filteredSportMapping = string.IsNullOrWhiteSpace(_searchQuery)
                ? _sportMapping
                : _sportMapping.Where(item => item.SpNa.Contains(_searchQuery)).ToList();

            _shouldRender = true;
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

            ModalService.Open<EditMappingSportModal>("Edit Mapping", parameters, options);
        }

        public async Task HandleDeleteMappingClickEvent(SportMappingModel mappingItem)
        {
            var parameters = new ModalParameters();
            parameters.Add("DialogStyle", "max-width: 360px");
            parameters.Add("SpId", mappingItem.SpId);
            var options = new ModalOptions
            {
                HideHeader = true
            };

            ModalService.Open<DeleteMappingSportModal>("Delete Mapping", parameters, options);
        }

        public async Task OnSaveClickEvent()
        {
            _searchQuery = string.Empty;
            await FetchSportMapping();

            _countCallbackModel = new UpdateMappingTabCountModel
            {
                Tab = MappingTab.Mapped,
                Count = _sportMapping.Count()
            };

            await OnItemsCountUpdateCallback.InvokeAsync(_countCallbackModel);

            _shouldRender = true;
        }
    }
}