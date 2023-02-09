using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.SBC.TopLeagues
{
    public partial class EditFilter : IAsyncDisposable, IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }

        [Parameter] public int ClientId { get; set; }
        [Parameter] public int SportId { get; set; }
        [Parameter] public EventCallback<EditTopLeaguesModel> OnInitCallback { get; set; }

        public SportsTreeModel SportsTree { get; set; }
        public SportsTreeModel.SportModel SelectedSport { get; set; }
        public SportsTreeModel.SegmentModel SelectedSegment { get; set; }
        public SportsTreeModel.LeagueModel SelectedLeague { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SelectedSport = new SportsTreeModel.SportModel();
            SelectedSegment = new SportsTreeModel.SegmentModel();
            SelectedLeague = new SportsTreeModel.LeagueModel();
            SportsTree = new SportsTreeModel();

            SportsTree = (await BackOfficeService.GetSportstreeByClientId(ClientId)).Result;

            if (SportsTree != null)
                SelectedSport = SportsTree.Sports.FirstOrDefault(s => s.SportId == SportId);

            var model = new EditTopLeaguesModel
            {
                Sports = SportsTree.Sports ?? new List<SportsTreeModel.SportModel>(),
                SelectedSport = SelectedSport,
                SelectedSegment = SelectedSegment,
                SelectedLeague = SelectedLeague
            };

            await OnInitCallback.InvokeAsync(model);
        }

        public async void OnSegmentChangedEvent(ChangeEventArgs e)
        {
            SelectedSegment = SelectedSport.Segments.FirstOrDefault(s => s.SegmentId == Convert.ToInt32(e.Value));
            SelectedLeague = new SportsTreeModel.LeagueModel();

            var model = new EditTopLeaguesModel
            {
                Sports = SportsTree.Sports,
                SelectedSport = SelectedSport,
                SelectedSegment = SelectedSegment,
                SelectedLeague = SelectedLeague
            };

            await OnInitCallback.InvokeAsync(model);
        }

        public async void OnLeagueChangedEvent(ChangeEventArgs e)
        {
            SelectedLeague = SelectedSegment.Leagues.FirstOrDefault(s => s.LeagueId == Convert.ToInt32(e.Value));
            var model = new EditTopLeaguesModel
            {
                Sports = SportsTree.Sports,
                SelectedSport = SelectedSport,
                SelectedSegment = SelectedSegment,
                SelectedLeague = SelectedLeague
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
