using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Mapping;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Mapping.League
{
    public partial class MappedLeague
    {
        [Inject] public IModalService ModalService { get; set; }

        [Parameter] public EventCallback<UpdateMappingTabCountModel> OnItemsCountUpdateCallback { get; set; }

        private string _searchQuery { get; set; } = string.Empty;
        private List<LeagueMappingModel> _leagueMapping = new();
        private List<LeagueMappingModel> _filteredLeagueMapping = new();
        private UpdateMappingTabCountModel _countCallbackModel { get; set; }
        private bool _shouldRender;

        protected override bool ShouldRender() => _shouldRender;

        protected override async Task OnInitializedAsync()
        {
            await FetchLeagueMapping();

            _shouldRender = true;
        }

        private async Task FetchLeagueMapping()
        {
            var request = new MappingRequest
            {
                Page = 1,
                PageSize = 1000
            };

            var response = await _providerMappingService.GetLeaguesMapping(request);
            _leagueMapping = response.Result.LeagueMapping.OrderBy(sm => sm.LeNa).ToList();
            _filteredLeagueMapping = _leagueMapping;

            _shouldRender = true;
        }
        private void PerformSearch(ChangeEventArgs e)
        {
            _searchQuery = e.Value?.ToString();
            _filteredLeagueMapping = string.IsNullOrWhiteSpace(_searchQuery)
                ? _leagueMapping
                : _leagueMapping.Where(item => item.LeNa.Contains(_searchQuery)).ToList();
            _shouldRender = true;
        }


        public async Task HandleEditMappingClickEvent(LeagueMappingModel mappingItem)
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("LeId", mappingItem.LeId);
            var options = new ModalOptions
            {
                HideHeader = true
            };
            ModalService.Open<EditMappingLeagueModal>("Edit League Mapping", parameters, options);

        }
        public async Task HandleDeleteMappingClickEvent(LeagueMappingModel mappingItem)
        {
            var parameters = new ModalParameters();
            parameters.Add("DialogStyle", "max-width: 360px");
            parameters.Add("LeId", mappingItem.LeId);
            parameters.Add("LeNa", mappingItem.LeNa);
            var options = new ModalOptions
            {
                HideHeader = true
            };
            ModalService.Open<DeleteMappingLeagueModal>("Delete League Mapping", parameters, options);

        }
        public async Task OnSaveClickEvent()
        {
            _searchQuery = string.Empty;
            await FetchLeagueMapping();

            _countCallbackModel = new UpdateMappingTabCountModel
            {
                Tab = MappingTab.Mapped,
                Count = _leagueMapping.Count()
            };

            await OnItemsCountUpdateCallback.InvokeAsync(_countCallbackModel);

            _shouldRender = true;
        }

    }
}
