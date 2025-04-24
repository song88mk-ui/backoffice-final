using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Mapping;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;


namespace Backoffice.Web.Components.OddsEngine.Mapping.Segment
{
    public partial class MappedSegment
    {
        [Inject] public IModalService ModalService { get; set; }

        [Parameter] public EventCallback<UpdateMappingTabCountModel> OnItemsCountUpdateCallback { get; set; }

        private string _searchQuery { get; set; } = string.Empty;
        private List<SegmentMappingModel> _segmentMapping = new List<SegmentMappingModel>();
        private List<SegmentMappingModel> _filteredSegmentMapping = new List<SegmentMappingModel>();
        private UpdateMappingTabCountModel _countCallbackModel { get; set; }
        private bool _shouldRender;

        protected override bool ShouldRender() => _shouldRender;
        protected override async Task OnInitializedAsync()
        {
            await FetchSegmentMapping();

            _shouldRender = true;
        }
        
        private async Task FetchSegmentMapping()
        {
            var request = new MappingRequest
            {
                Page = 1,
                PageSize = 1000
            };

            var response = await _providerMappingService.GetSegmentMapping(request);
            _segmentMapping = response.Result.SegmentMapping.OrderBy(sm => sm.SegNa).ToList();
            _filteredSegmentMapping = _segmentMapping;

            _shouldRender = true;

        }
        private void PerformSearch(ChangeEventArgs e)
        {
            _searchQuery = e.Value?.ToString();
            _filteredSegmentMapping = string.IsNullOrWhiteSpace(_searchQuery)
                ? _segmentMapping
                : _segmentMapping.Where(item => item.SegNa.Contains(_searchQuery)).ToList();
            _shouldRender = true;
        }


        public async Task HandleEditMappingClickEvent(SegmentMappingModel mappingItem)
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("SegId", mappingItem.SegId);
            var options = new ModalOptions
            {
                HideHeader = true
            };
            ModalService.Open<EditMappingSegmentModal>("Edit Segment Mapping", parameters, options);

        }
        public async Task HandleDeleteMappingClickEvent(SegmentMappingModel mappingItem)
        {
            var parameters = new ModalParameters();
            parameters.Add("DialogStyle", "max-width: 360px");
            parameters.Add("SegId", mappingItem.SegId);
            parameters.Add("SegNa", mappingItem.SegNa);
            var options = new ModalOptions
            {
                HideHeader = true
            };
            ModalService.Open<DeleteMappingSegmentModal>("Delete Segment Mapping", parameters, options);

        }
        public async Task OnSaveClickEvent()
        {
            _searchQuery = string.Empty;
            await FetchSegmentMapping();

            _countCallbackModel = new UpdateMappingTabCountModel
            {
                Tab = MappingTab.Mapped,
                Count = _segmentMapping.Count()
            };

            await OnItemsCountUpdateCallback.InvokeAsync(_countCallbackModel);

            _shouldRender = true;
        }

    }
}