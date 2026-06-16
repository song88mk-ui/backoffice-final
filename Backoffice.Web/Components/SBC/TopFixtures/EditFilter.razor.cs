using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using SportModel = Backoffice.Web.Models.SportModel;

namespace Backoffice.Web.Components.SBC.TopFixtures
{
    public partial class EditFilter : IAsyncDisposable, IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }

        [Parameter] public int ClientId { get; set; }
        [Parameter] public EventCallback<EditTopFixturesModel> OnInitCallback { get; set; }

        public SportsTreeModel SportsTree { get; set; }
        public SportModel SelectedSport { get; set; }
        public SegmentModel SelectedSegment { get; set; }
        public LeagueModel SelectedLeague { get; set; }
        public FixtureModel SelectedGameFilter { get; set; }

        public string SortFilter { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                SelectedSport = new SportModel();
                SelectedSegment = new SegmentModel();
                SelectedLeague = new LeagueModel();
                SelectedGameFilter = new FixtureModel();
                SportsTree = new SportsTreeModel();
                SortFilter = "1";

                var sportstreeResponse = await BackofficeService.GetSportstreeByClientId(ClientId);

                SportsTree = sportstreeResponse?.Result ?? new SportsTreeModel();
                SportsTree.Sports ??= new List<SportModel>();

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

            SelectedSport = SportsTree.Sports.FirstOrDefault(c => c.SportId == Convert.ToInt32(e.Value));
            if (SelectedSport == null)
                SelectedSport = new SportModel();

            SelectedSegment = new SegmentModel();
            SelectedLeague = new LeagueModel();
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
            SelectedSegment = SelectedSport.Segments.FirstOrDefault(c => c.SegmentId == Convert.ToInt32(e.Value));
            if (SelectedSegment == null)
                SelectedSegment = new SegmentModel();

            SelectedLeague = new LeagueModel();
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
            SelectedLeague = SelectedSegment.Leagues.FirstOrDefault(c => c.LeagueId == Convert.ToInt32(e.Value));
            if (SelectedLeague == null)
                SelectedLeague = new LeagueModel();
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
            SelectedGameFilter = SelectedLeague.Fixtures.FirstOrDefault(c => c.FixtureId == e.Value);
            if (SelectedGameFilter == null)
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
