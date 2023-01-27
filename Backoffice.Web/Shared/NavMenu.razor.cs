using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;

namespace Backoffice.Web.Shared
{
    public partial class NavMenu : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        [Parameter] public NavMenuAccess NavMenuAccess { get; set; }
        [Parameter] public EventCallback<SportClickCallbackModel> OnSportClickCallback { get; set; }

        public IEnumerable<Claim> Claims { get; set; }
        public SportClickCallbackModel SelectedSport { get; set; }

        public SportsTreeModel Menu { get; set; }

        #endregion

        #region Fields

        private bool collapseNavMenu = true;

        #endregion

        #region Methods

        protected override async void OnInitialized()
        {
            Menu = new SportsTreeModel();
            SelectedSport = new SportClickCallbackModel();

            if (NavMenuAccess == NavMenuAccess.OE)
            {
                await GetSportsTreeMenu();

                var sport = Menu.Sports.FirstOrDefault();

                if (sport != null)
                {
                    SelectedSport.SportId = sport.SportId;
                    SelectedSport.SportName = sport.SportName;

                    var segment = sport.Segments.FirstOrDefault();

                    if (segment != null)
                    {
                        SelectedSport.SegmentId = segment.SegmentId;
                        SelectedSport.SegmentName = segment.SegmentName;

                        var league = segment.Leagues.FirstOrDefault();

                        if (league != null)
                        {
                            SelectedSport.LeagueId = league.LeagueId;
                            SelectedSport.LeagueName = league.LeagueName;
                        }
                    }
                }

                await OnSportClickCallback.InvokeAsync(SelectedSport);
                StateHasChanged();
            }

            if (UserModel != null)
            {
                if (UserModel.Claims != null && Claims == null)
                    Claims = UserModel.Claims;
            }
        }
        public void ToggleNavMenu()
        {
            collapseNavMenu = !collapseNavMenu;
        }

        public async Task OnSportClickEvent(int sportId)
        {
            if (SelectedSport.SportId != sportId)
            {
                var sport = Menu.Sports.FirstOrDefault(c => c.SportId == sportId);

                SelectedSport.SportId = sport.SportId;
                SelectedSport.SportName = sport.SportName;
            }
            else
            {
                SelectedSport.SportId = 0;
                SelectedSport.SportName = string.Empty;
            }

            // reset selected segment and league if new sport has been selected. 
            SelectedSport.SegmentId = 0;
            SelectedSport.SegmentName = string.Empty;

            SelectedSport.LeagueId = 0;
            SelectedSport.LeagueName = string.Empty;
        }

        public async Task OnSegmentClickEvent(int segmentId)
        {
            if (SelectedSport.SegmentId != segmentId)
            {
                var sport = Menu.Sports.FirstOrDefault(c => c.SportId == SelectedSport.SportId);
                var segment = sport.Segments.FirstOrDefault(c => c.SegmentId == segmentId);

                SelectedSport.SegmentId = segment.SegmentId;
                SelectedSport.SegmentName = segment.SegmentName;

                var league = segment.Leagues.FirstOrDefault();

                SelectedSport.LeagueId = league.LeagueId;
                SelectedSport.LeagueName = league.LeagueName;

                // verify if you're in the monitoring page, if not then the user will be navigated.
                var slugs = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "").Split("/").ToList();

                if (slugs.Count != 1 && slugs[1].ToLower() != "monitoring")
                {
                    NavigationManager.NavigateTo("/odds-engine");
                }

                await OnSportClickCallback.InvokeAsync(SelectedSport);
            }
            else
            {
                SelectedSport.SegmentId = 0;
                SelectedSport.SegmentName = string.Empty;

                SelectedSport.LeagueId = 0;
                SelectedSport.LeagueName = string.Empty;
            }
        }

        public async Task OnLeagueClickEvent(int leagueId)
        {
            if (SelectedSport.LeagueId != leagueId)
            {
                var sport = Menu.Sports.FirstOrDefault(c => c.SportId == SelectedSport.SportId);
                var segment = sport.Segments.FirstOrDefault(c => c.SegmentId == SelectedSport.SegmentId);
                var league = segment.Leagues.FirstOrDefault(c => c.LeagueId == leagueId);

                SelectedSport.LeagueId = league.LeagueId;
                SelectedSport.LeagueName = league.LeagueName;

                // verify if you're in the monitoring page, if not then the user will be navigated.
                var slugs = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "").Split("/").ToList();

                if (slugs.Count != 1 && slugs[1].ToLower() != "monitoring")
                {
                    NavigationManager.NavigateTo("/odds-engine");
                }

                await OnSportClickCallback.InvokeAsync(SelectedSport);
            }
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private async Task GetSportsTreeMenu()
        {
            var response = await BackOfficeService.GetSportsTreeAsync((int)FixtureType.Match);

            if (response.StatusCode == StatusCode.Success)
            {
                Menu.Sports = response.Result.Sports
                    .Select(sp => new SportsTreeModel.SportModel
                    {
                        SportId = sp.SportId,
                        SportName = sp.SportName,
                        SortOrder = sp.SortOrder,
                        Segments = sp.Segments.Select(se => new SportsTreeModel.SegmentModel
                        {
                            SegmentId = se.SegmentId,
                            SegmentName = se.SegmentName,
                            SortOrder = se.SortOrder,
                            Leagues = se.Leagues.Select(le => new SportsTreeModel.LeagueModel
                            {
                                LeagueId = le.LeagueId,
                                LeagueName = le.LeagueName,
                                SortOrder = le.SortOrder,
                            }).OrderBy(o => o.LeagueName)
                            .ToList()
                        }).OrderBy(o => o.SegmentName)
                        .ToList()
                    })
                    .OrderBy(o => o.SportName)
                    .ToList();
            }
        }

        #endregion
    }
}
