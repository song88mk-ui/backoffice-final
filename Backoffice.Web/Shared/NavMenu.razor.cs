using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Shared
{
    public partial class NavMenu : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        [Parameter] public NavMenuAccess NavMenuAccess { get; set; }
        [CascadingParameter(Name = "OnSportClickCallback")] public EventCallback<SportClickCallbackModel> OnSportClickCallback { get; set; }
        [CascadingParameter(Name = "DefaultSport")] public SportClickCallbackModel DefaultSport { get; set; }
        public SportsTreeModel Menu { get; set; }
        #endregion

        #region Fields

        private bool collapseNavMenu = true;
        private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;


        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Menu = new SportsTreeModel();

            if (DefaultSport == null)
                DefaultSport = new SportClickCallbackModel();

            await GetSportsTreeMenu();

        }

        public void ToggleNavMenu()
        {
            collapseNavMenu = !collapseNavMenu;
        }

        public async Task OnSportClickEvent(int sportId)
        {
            DefaultSport.SegmentId = DefaultSport.SportId == sportId ? 0 : DefaultSport.SportId != sportId ? 0 : DefaultSport.SegmentId;
            DefaultSport.SportId = DefaultSport.SportId == sportId ? 0 : sportId;
        }

        public async Task OnSegmentClickEvent(int segmentId)
        {
            if (DefaultSport.SegmentId == segmentId)
            {
                DefaultSport.SegmentId = 0;
                DefaultSport.LeagueId = 0;
                return;
            }

            DefaultSport.SegmentId = segmentId;

            var sport = Menu.Sports.FirstOrDefault(c => c.SportId == DefaultSport.SportId);
            var segment = sport.Segments.FirstOrDefault(c => c.SegmentId == DefaultSport.SegmentId);
            var league = segment.Leagues.FirstOrDefault();

            DefaultSport.LeagueId = league.LeagueId;

            await OnSportClickCallback.InvokeAsync(new SportClickCallbackModel
            {
                SportId = DefaultSport.SportId,
                SegmentId = DefaultSport.SegmentId,
                LeagueId = DefaultSport.LeagueId
            });
        }

        public async Task OnLeagueClickEvent(int leagueId)
        {
            DefaultSport.LeagueId = leagueId;

            await OnSportClickCallback.InvokeAsync(new SportClickCallbackModel
            {
                SportId = DefaultSport.SportId,
                SegmentId = DefaultSport.SegmentId,
                LeagueId = DefaultSport.LeagueId
            });
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private async Task GetSportsTreeMenu()
        {
            var result = new SportsTreeModel();

            var response = await BackOfficeService.GetSportsTreeAsync((int)FixtureType.Match);

            foreach (var sport in response.Result.Sports)
            {
                result.Sports.Add(new SportsTreeModel.SportModel
                {
                    SportId = sport.SportId,
                    SportName = sport.SportName,
                    SortOrder = sport.SortOrder,
                    Segments = sport.Segments.Select(se => new SportsTreeModel.SegmentModel
                    {
                        SegmentId = se.SegmentId,
                        SegmentName = se.SegmentName,
                        SortOrder = se.SortOrder,
                        Leagues = se.Leagues.Select(le => new SportsTreeModel.LeagueModel
                        {
                            LeagueId = le.LeagueId,
                            LeagueName = le.LeagueName,
                            SortOrder = le.SortOrder,
                        }).OrderBy(o => o.SortOrder).ToList()
                    }).OrderBy(o => o.SortOrder).ToList()
                });
            }

            if (result.Sports.Any())
                result.Sports = result.Sports.OrderBy(o => o.SportName).ToList();

            Menu = result;
        }

        #endregion
    }
}
