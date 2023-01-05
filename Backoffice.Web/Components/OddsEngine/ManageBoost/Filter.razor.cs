using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.ManageBoost
{
    public partial class Filter : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }
        [Parameter] public PriceboostFilterModel PriceboostFilterModel { get; set; }
        [Parameter] public EventCallback<string> OnClientChangedCallback { get; set; }
        [Parameter] public EventCallback<int> OnSportChangedCallback { get; set; }
        [Parameter] public EventCallback<Tuple<int,int>> OnSegmentChangedCallback { get; set; }
        [Parameter] public EventCallback OnLeagueChangedCallback { get; set; }        
        //[Parameter] public EventCallback OnFilterChangedCallback { get; set; }

        public SearchFormModel SearchForm { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SearchForm = new SearchFormModel();

            //PriceboostFilterModel = new PriceboostFilterModel();

            //PriceboostFilterModel.Sports =  await LoadSports();
            //PriceboostFilterModel.Segments = await LoadSegments(PriceboostFilterModel.SportId);
            //PriceboostFilterModel.Leagues = await LoadLeagues(PriceboostFilterModel.SportId, PriceboostFilterModel.SegmentId);
        }

        public async Task OnInputSearchEvent()
        {
        }

        public async Task<List<Models.SelectListItemModel<int>>> LoadSports()
        {
            var result = (await TradingService.GetSportLookup()).Result;

            if (result == null)
                return new List<Models.SelectListItemModel<int>>();

            var sports = result.SportLookup.Select(s => new Models.SelectListItemModel<int>()
            {
                Value = s.SportId,
                Name = s.SportName
            }).ToList();

            return sports;      
        }

        public async Task<List<Models.SelectListItemModel<int>>> LoadSegments(int sportId)
        {
            var result = (await TradingService.GetSegmentLookup(sportId)).Result;

            if (result == null)
                return new List<Models.SelectListItemModel<int>>();

            var segments = result.SegmentLookup.Select(s => new Models.SelectListItemModel<int>()
            {
                Value = s.SegmentId,
                Name = s.SegmentName
            }).ToList();

            return segments;
        }

        public async Task<List<Models.SelectListItemModel<int>>> LoadLeagues(int sportId, int segmentId)
        {
            var result = (await TradingService.GetLeagueLookup(sportId, segmentId)).Result;

            if (result == null)
                return new List<Models.SelectListItemModel<int>>();

            var leagues = result.LeagueLookup.Select(s => new Models.SelectListItemModel<int>()
            {
                Value = s.LeagueId,
                Name = s.LeagueName
            }).ToList();

            return leagues;
        }

        public async Task OnAddBoostedOdds()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("PriceBoostFilterModel", PriceboostFilterModel);            

            var options = new ModalOptions();
            ModalService.Open<AddBoostedOddsModal>($"Add Boosted Odds", parameters, options);
        }

        public async Task OnViewAllLogs()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle","max-width: 65rem;");

            var options = new ModalOptions();
            ModalService.Open<ViewAllLogsModal>($"View Logs", parameters, options);
        }

        public async Task OnClientChangedEvent(ChangeEventArgs e)
        {
            PriceboostFilterModel.ClientCode = e.Value.ToString();

            await OnClientChangedCallback.InvokeAsync(PriceboostFilterModel.ClientCode);
        }

        public async Task OnSportChangedEvent(ChangeEventArgs e)
        {
            PriceboostFilterModel.SportId = int.Parse(e.Value.ToString());

            await OnSportChangedCallback.InvokeAsync(PriceboostFilterModel.SportId);
        }

        public async Task OnSegmentChangedEvent(ChangeEventArgs e)
        {
            PriceboostFilterModel.SegmentId = int.Parse(e.Value.ToString());

            await OnSegmentChangedCallback.InvokeAsync(new Tuple<int, int>(PriceboostFilterModel.SportId, PriceboostFilterModel.SegmentId));
        }

        public async Task OnLeagueChangedEvent(ChangeEventArgs e)
        {
            PriceboostFilterModel.LeagueId = int.Parse(e.Value.ToString());

            await OnLeagueChangedCallback.InvokeAsync();
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
