using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Mapping;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using DebounceTimer = System.Timers.Timer;

namespace Backoffice.Web.Components.OddsEngine.Mapping.Sport
{
    public partial class MappedSport
    {
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        private List<SportMappingModel> _sportMapping = new();
        private string _searchText = string.Empty;
        private DebounceTimer _debounceTimer;
        protected override async Task OnInitializedAsync()
        {
            await FetchSportMapping();

            RenderComponent = true;
        }

        private async Task FetchSportMapping()
        {
            var request = new MappingRequest
            {
                Page = 1,
                PageSize = 1000,
                SearchText = _searchText
            };

            var response = await _providerMappingService.GetSportMapping(request);
            _sportMapping = response.Result.SportMapping.OrderBy(sm => sm.SpNa).ToList();
        }

        private void OnSearchTextChanged(ChangeEventArgs e)
        {
            _searchText = e.Value.ToString();

            _debounceTimer?.Stop();
            _debounceTimer = new DebounceTimer(1000);
            _debounceTimer.Elapsed += async (_,_) =>
            {
                _debounceTimer.Stop();
                await FetchSportMapping();
                InvokeAsync(StateHasChanged);
            };
            _debounceTimer.Enabled = true;
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
            _searchText = string.Empty;
            await FetchSportMapping();

            CountCallbackModel = new UpdateMappingTabCountModel
            {
                Tab = MappingTab.Mapped,
                Count = _sportMapping.Count()
            };

            await OnItemsCountUpdateCallback.InvokeAsync(CountCallbackModel);
        }
    }
}