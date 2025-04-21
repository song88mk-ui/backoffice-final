using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.OddsEngine.Mapping.League;
using System.Text.Json;


namespace Backoffice.Web.Components.OddsEngine.Mapping.Segment
{
    public partial class MappedSegment
    {
        private bool _shouldRender;
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [Parameter] public EventCallback<bool> OnSuccessSaveCallback { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        protected override bool ShouldRender() => _shouldRender;
        private List<SegmentMappingModel> _leagueMapping = new();
        private List<SegmentMappingModel> _filteredLeagueMapping = new();

        private string _searchQuery { get; set; } = string.Empty;
       protected override async Task OnInitializedAsync()
        {
            await FetchSegmentMapping();
        }
        
        private async Task FetchSegmentMapping()
        {
            var request = new MappingRequest
            {
                Page = 1,
                PageSize = 1000
            };

            var response = await _providerMappingService.GetSegmentMapping(request);
            _leagueMapping = response.Result.SegmentMapping.OrderBy(sm => sm.SegNa).ToList();
            _filteredLeagueMapping = _leagueMapping;
            _shouldRender = true;

        }
        private void PerformSearch(ChangeEventArgs e)
        {
            _searchQuery = e.Value?.ToString();
            _filteredLeagueMapping = string.IsNullOrWhiteSpace(_searchQuery)
                ? _leagueMapping
                : _leagueMapping.Where(item => item.SegNa.Contains(_searchQuery)).ToList();
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
            ModalService.Open<EditMappingLeagueModal>("Edit League Mapping", parameters, options);

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
            ModalService.Open<DeleteMappingLeagueModal>("Delete League Mapping", parameters, options);

        }
        // public async Task OnSaveClickEvent()
        // {
        //     OnSuccessSaveCallback.InvokeAsync(true);
        //     await FetchLeagueMapping();
        // }

    }
}
