using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Drawing;

namespace Backoffice.Web.Shared
{
    public partial class NavMenu : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        [Parameter] public PermissionCategory NavMenuAccess { get; set; }
        [Parameter] public NavMenuModel NavMenus { get; set; }
        [Parameter] public EventCallback<SportClickCallbackModel> OnSportClickCallback { get; set; }

        [Parameter] public string Region { get; set; }
        public SportClickCallbackModel SelectedSport { get; set; }

        public SportsTreeModel Menu { get; set; }

        //private List<string> _dissabledMenus = new List<string>() { "menu.sbc.deeplinkgenerator", "menu.sbc.allbets", "menu.sbc.winloss" , "menu.sbc.profitloss" ,
        //    "menu.sbc.ggr" , "sub-menu-revenue-settlement",  "menu.admin.createuser","menu.admin.createusergroup", "menu.admin.settlements.manualbetsettlementhistory"};

        #endregion

        #region Fields

        private bool collapseNavMenu = true;

        #endregion

        #region Methods

        protected override async void OnInitialized()
        {
            Menu = new SportsTreeModel();
            SelectedSport = new SportClickCallbackModel();

            var page = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "");
            var splitVal = page.Split("/").FirstOrDefault();

            if (splitVal != null)
            {
                var regions = new List<string>() { "HK", "SA" };
                Region = regions.Contains(splitVal.Trim().ToUpper()) ? splitVal.Trim().ToUpper() : null;
            }

            if (NavMenuAccess == PermissionCategory.OE)
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

                switch(page.ToLower())
                {
                    case "sa/odds-engine":
                    case "hk/odds-engine":
                    case "sa/odds-engine/monitoring":
                    case "hk/odds-engine/monitoring":
                        await OnSportClickCallback.InvokeAsync(SelectedSport);
                        break;
                }
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
                    NavigationManager.NavigateTo($"/{Region}/odds-engine");
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
                    NavigationManager.NavigateTo($"/{Region}/odds-engine");
                }

                await OnSportClickCallback.InvokeAsync(SelectedSport);
            }
        }

        public void Dispose()
        {
            BackOfficeService.Dispose();
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
