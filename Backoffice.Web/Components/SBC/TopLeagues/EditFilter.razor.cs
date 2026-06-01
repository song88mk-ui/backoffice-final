using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.SBC.TopLeagues
{
    public partial class EditFilter : IAsyncDisposable, IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }

        [Parameter] public int ClientId { get; set; }
        [Parameter] public int SportId { get; set; }
        [Parameter] public EventCallback<EditTopLeaguesModel> OnInitCallback { get; set; }

        public SportsTreeModel SportsTree { get; set; }
        public SportModel SelectedSport { get; set; }
        public SegmentModel SelectedSegment { get; set; }
        public LeagueModel SelectedLeague { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SelectedSport = new SportModel();
            SelectedSegment = new SegmentModel();
            SelectedLeague = new LeagueModel();
            SportsTree = new SportsTreeModel();
            ErrorMessage = string.Empty;
            try
            {
                SportsTree = (await BackofficeService.GetSportstreeByClientId(ClientId)).Result;
                if (SportsTree != null)
                    SelectedSport = SportsTree.Sports.FirstOrDefault(s => s.SportId == SportId);

                var model = new EditTopLeaguesModel
                {
                    Sports = SportsTree.Sports ?? new List<SportModel>(),
                    SelectedSport = SelectedSport,
                    SelectedSegment = SelectedSegment,
                    SelectedLeague = SelectedLeague
                };   
                await OnInitCallback.InvokeAsync(model);         
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading data: {ex.Message}";
            }

        }

        public async void OnSegmentChangedEvent(ChangeEventArgs e)
        {
            SelectedSegment = SelectedSport.Segments.FirstOrDefault(s => s.SegmentId == Convert.ToInt32(e.Value));
            SelectedLeague = new LeagueModel();

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
