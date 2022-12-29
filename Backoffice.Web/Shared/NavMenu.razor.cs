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
        [Parameter] public EventCallback<SportClickCallbackModel> OnSportClickCallback { get; set; }
        public SportsTreeModel Menu { get; set; }
        public SportsTreeModel.SportModel DefaultSport { get; set; }
        #endregion

        #region Fields

        private bool collapseNavMenu = true;
        private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

        public string _sportId;
        public string _segmentId;
        public string _leagueId;

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {

            Menu = new SportsTreeModel();

            await GetSportsTreeMenu();

            DefaultSport = new SportsTreeModel.SportModel();

            if (!string.IsNullOrEmpty(_sportId))
            {
                DefaultSport.SportId = Convert.ToInt32(_sportId);

                if (!string.IsNullOrEmpty(_segmentId))
                {
                    DefaultSport.Segments = new List<SportsTreeModel.SegmentModel>();

                    var leagues = new List<SportsTreeModel.LeagueModel>();
                    leagues.Add(new SportsTreeModel.LeagueModel
                    {
                        LeagueId = string.IsNullOrEmpty(_leagueId) ? 0 : Convert.ToInt32(_leagueId)
                    });

                    DefaultSport.Segments.Add(new SportsTreeModel.SegmentModel
                    {
                        SegmentId = Convert.ToInt32(_segmentId),
                        Leagues = leagues
                    });
                }
            }
        }

        public void ToggleNavMenu()
        {
            collapseNavMenu = !collapseNavMenu;
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
