using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.TopFixtures
{
    public partial class FilterEdit : IAsyncDisposable, IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Parameter] public int ClientId { get; set; }
        [Parameter] public EventCallback<TopFixtureCallback> OnInitCallback { get; set; }
        [Parameter] public EventCallback<string> OnSearchCallback { get; set; }
        public SportsTreeResponse SportsTree { get; set; }
        public SportsTreeResponse.SportReponse SelectedSport { get; set; }
        public SportsTreeResponse.SegmentResponse SelectedSegment { get; set; }
        public SportsTreeResponse.LeagueResponse SelectedLeague { get; set; }
        public FixtureModel SelectedGame { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                SelectedSport = new SportsTreeResponse.SportReponse();
                SelectedSegment = new SportsTreeResponse.SegmentResponse();
                SelectedLeague = new SportsTreeResponse.LeagueResponse();
                SelectedGame = new FixtureModel();
                SportsTree = new SportsTreeResponse();

                SportsTree = (await BackOfficeService.GetSportstreeByClientId(ClientId)).Result;
            }
            catch (Exception)
            {
            }
        }

        public async void OnSportChangedEvent(ChangeEventArgs e)
        {
            SelectedSport = SportsTree.Sports.FirstOrDefault(s => s.SportId == Convert.ToInt32(e.Value));
            SelectedSegment = new SportsTreeResponse.SegmentResponse();
            SelectedLeague = new SportsTreeResponse.LeagueResponse();
            SelectedGame = new FixtureModel();
            //await OnInitCallback.InvokeAsync(new TopFixtureCallback
            //{
            //    SelectedClients = selectedClients,
            //    SearchText = SearchForm.SearchText
            //});
        }
        public async void OnSegmentChangedEvent(ChangeEventArgs e)
        {
            SelectedSegment = SelectedSport.Segments.FirstOrDefault(s => s.SegmentId == Convert.ToInt32(e.Value));
            SelectedLeague = new SportsTreeResponse.LeagueResponse();
            SelectedGame = new FixtureModel();
            //await OnInitCallback.InvokeAsync(new TopFixtureCallback
            //{
            //    SelectedClients = selectedClients,
            //    SearchText = SearchForm.SearchText
            //});
        }

        public async void OnLeagueChangedEvent(ChangeEventArgs e)
        {
            SelectedLeague = SelectedSegment.Leagues.FirstOrDefault(s => s.LeagueId == Convert.ToInt32(e.Value));
            SelectedGame = new FixtureModel();
            //await OnInitCallback.InvokeAsync(new TopFixtureCallback
            //{
            //    SelectedClients = selectedClients,
            //    SearchText = SearchForm.SearchText
            //});
        }

        public async void OnGameChangedEvent(ChangeEventArgs e)
        {
            SelectedGame = SelectedLeague.Fixtures.FirstOrDefault(f => f.FixtureId == e.Value);
            //await OnInitCallback.InvokeAsync(new TopFixtureCallback
            //{
            //    SelectedClients = selectedClients,
            //    SearchText = SearchForm.SearchText
            //});
        }

        public async ValueTask DisposeAsync()
        {
            
            GC.SuppressFinalize(this);
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
