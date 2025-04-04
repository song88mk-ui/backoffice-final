using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.OddsEngine.Mapping.Common;

namespace Backoffice.Web.Components.OddsEngine.Mapping.League
{
    public partial class MappedLeague
    {
        private bool _shouldRender;
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [Parameter] public EventCallback<bool> OnSuccessSaveCallback { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        protected override bool ShouldRender() => _shouldRender;
        private List<LeagueMappingModel> _leagueMapping = new();
        private List<LeagueMappingModel> _filteredLeagueMapping = new();
        private string _searchQuery { get; set; } = string.Empty;
       protected override async Task OnInitializedAsync()
        {
            await FetchLeagueMapping();
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
            parameters.Add("ModalDialogClass", "modal-lg");
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
            OnSuccessSaveCallback.InvokeAsync(true);
            await FetchLeagueMapping();
        }

    }
}
