using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.TopFixtures
{
    public partial class EditFilter : IAsyncDisposable, IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Parameter] public int ClientId { get; set; }
        [Parameter] public EventCallback<EditTopFixturesModel> OnInitCallback { get; set; }
        public SportsTreeResponse SportsTree { get; set; }
        public SportsTreeResponse.SportReponse SelectedSport { get; set; }
        public SportsTreeResponse.SegmentResponse SelectedSegment { get; set; }
        public SportsTreeResponse.LeagueResponse SelectedLeague { get; set; }
        public FixtureModel SelectedGameFilter { get; set; }
        public string SortFilter { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                SelectedSport = new SportsTreeResponse.SportReponse();
                SelectedSegment = new SportsTreeResponse.SegmentResponse();
                SelectedLeague = new SportsTreeResponse.LeagueResponse();
                SelectedGameFilter = new FixtureModel();
                SportsTree = new SportsTreeResponse();
                SortFilter = "1";

                SportsTree = (await BackOfficeService.GetSportstreeByClientId(ClientId)).Result;

                var model = new EditTopFixturesModel
                {
                    Sports = SportsTree.Sports,
                    SelectedSport = SelectedSport,
                    SelectedSegment = SelectedSegment,
                    SelectedLeague = SelectedLeague,
                    SelectedGameFilter = SelectedGameFilter
                };
                await OnInitCallback.InvokeAsync(model);
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
            SelectedGameFilter = new FixtureModel();

            var model = new EditTopFixturesModel
            {
                Sports = SportsTree.Sports,
                SelectedSport = SelectedSport,
                SelectedSegment = SelectedSegment,
                SelectedLeague = SelectedLeague,
                SelectedGameFilter = SelectedGameFilter
            };

            await OnInitCallback.InvokeAsync(model);
        }
        public async void OnSegmentChangedEvent(ChangeEventArgs e)
        {
            SelectedSegment = SelectedSport.Segments.FirstOrDefault(s => s.SegmentId == Convert.ToInt32(e.Value));
            SelectedLeague = new SportsTreeResponse.LeagueResponse();
            SelectedGameFilter = new FixtureModel();

            var model = new EditTopFixturesModel
            {
                Sports = SportsTree.Sports,
                SelectedSport = SelectedSport,
                SelectedSegment = SelectedSegment,
                SelectedLeague = SelectedLeague,
                SelectedGameFilter = SelectedGameFilter
            };

            await OnInitCallback.InvokeAsync(model);
        }

        public async void OnLeagueChangedEvent(ChangeEventArgs e)
        {
            SelectedLeague = SelectedSegment.Leagues.FirstOrDefault(s => s.LeagueId == Convert.ToInt32(e.Value));
            SelectedGameFilter = new FixtureModel();
            var model = new EditTopFixturesModel
            {
                Sports = SportsTree.Sports,
                SelectedSport = SelectedSport,
                SelectedSegment = SelectedSegment,
                SelectedLeague = SelectedLeague,
                SelectedGameFilter = SelectedGameFilter
            };

            await OnInitCallback.InvokeAsync(model);
        }

        public async void OnGameChangedEvent(ChangeEventArgs e)
        {
            SelectedGameFilter = SelectedLeague.Fixtures.FirstOrDefault(f => f.FixtureId == e.Value);
            var model = new EditTopFixturesModel
            {
                Sports = SportsTree.Sports,
                SelectedSport = SelectedSport,
                SelectedSegment = SelectedSegment,
                SelectedLeague = SelectedLeague,
                SelectedGameFilter = SelectedGameFilter
            };

            await OnInitCallback.InvokeAsync(model);
        }

        public async void OnSortChangedEvent(ChangeEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Value?.ToString()))
                SortFilter = "1";
            SortFilter = e.Value.ToString();

            var model = new EditTopFixturesModel
            {
                Sports = SportsTree.Sports,
                SelectedSport = SelectedSport,
                SelectedSegment = SelectedSegment,
                SelectedLeague = SelectedLeague,
                SelectedGameFilter = SelectedGameFilter,
                SortFilter = SortFilter ?? "1"
            };

            await OnInitCallback.InvokeAsync(model);
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
