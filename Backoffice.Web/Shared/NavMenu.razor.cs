using Backoffice.Shared.Enums;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Monitoring;
using Backoffice.Web.Models.Shared;
using Microsoft.AspNetCore.Components;
using RabbitMQ.Client.MessagePatterns;
using System.Collections.Immutable;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace Backoffice.Web.Shared
{
    public partial class NavMenu : IDisposable
    {
        #region Properties

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        [Parameter] public NavMenuParameter NavMenuParameter { get; set; }
        [Parameter] public EventCallback<SportClickCallbackModel> OnSportClickCallback { get; set; }

        #endregion

        #region Fields

        private List<Claim> _userClaims = new List<Claim>();

        private PermissionCategory? _navMenuAccess;
        private LanguageCode _languageCode;

        private SportsTreeModel _menu = new SportsTreeModel();
        private SportClickCallbackModel _selectedSport = new SportClickCallbackModel();
        private NavMenuModel _navMenus;

        // New Sport Tree
        private List<SportItemModel> _sports = new();
        //private CurrentSportModel _currentSport = new();

        private string[] _permissionCategories = new string[] { nameof(PermissionCategory.Admin), nameof(PermissionCategory.SBC) };
        private string[] _access = new string[] { ((int)Access.View).ToString(), ((int)Access.Edit).ToString() };
        private readonly List<string> _monitoringUrls = new List<string>() { "hk/odds-engine", "hk/odds-engine/monitoring" };

        private string _page;

        private bool _showSwitchAccess;
        private bool collapseNavMenu = true;
        private bool _init = true;

        #endregion

        #region Methods

        protected override async Task OnParametersSetAsync()
        {
            var page = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "");

            if (!_userClaims.SequenceEqual(UserModel.Claims))
            {
                _userClaims = new List<Claim>(UserModel.Claims);

                var categories = UserModel.Claims?.Where(c => _access.Contains(c.Value)).Select(s => s.Type.Split(".")[0]).Distinct().ToList();
                categories = categories?.Where(c => _permissionCategories.Contains(c)).ToList();

                _showSwitchAccess = categories != null && categories.Count > 1;
            }

            var init = _init;
            //if (_page != null && _page != "hk/odds-engine" && !_page.Contains("hk/odds-engine/monitoring-details"))
            //{
            //    init = _init || _page != page;
            //}
            var languageCode = _languageCode;
            var navMenuAccess = _navMenuAccess;
            _page = page;

            if (init || _navMenuAccess != NavMenuParameter?.NavMenuAccess || languageCode != _translationState.ActiveLanguageCode)
            {
                var cts = new CancellationTokenSource();
                _languageCode = _translationState.ActiveLanguageCode;
                _navMenuAccess = NavMenuParameter?.NavMenuAccess;

                var categories = _userClaims?.Where(c => _access.Contains(c.Value)).Select(s => s.Type.Split(".")[0]).Distinct().ToList();
                categories = categories?.Where(c => _permissionCategories.Contains(c)).ToList();

                _showSwitchAccess = categories != null && categories.Count > 1;

                if (NavMenuParameter?.NavMenuAccess == PermissionCategory.OE)
                {
                    _sports = await GetSportTreeMenus(_translationState.ActiveLanguageCode, cts.Token);

                    if (cts.Token.IsCancellationRequested)
                        return;

                    if (init || navMenuAccess != NavMenuParameter?.NavMenuAccess)
                    {
                        _selectedSport = new();

                        var sport = _sports.FirstOrDefault();
                        if (sport != null)
                        {
                            sport.IsSelected = true;
                            _selectedSport.SportId = sport.Id;
                            _selectedSport.SportName = sport.Name;

                            var segment = sport.Segments.FirstOrDefault();
                            if (segment != null)
                            {
                                segment.IsSelected = true;
                                _selectedSport.SegmentId = segment.Id;
                                _selectedSport.SegmentName = segment.Name;

                                var league = segment.Leagues.FirstOrDefault();
                                if (league != null)
                                {
                                    league.IsSelected = true;
                                    _selectedSport.LeagueId = league.Id;
                                    _selectedSport.LeagueName = league.Name;
                                }
                            }
                        }

                        switch (_page?.ToLower() ?? string.Empty)
                        {
                            case "hk/odds-engine":
                            case "hk/odds-engine/monitoring":
                                await OnSportClickCallback.InvokeAsync(_selectedSport);
                                break;
                        }
                    }
                    else if (languageCode != _translationState.ActiveLanguageCode)
                    {
                        var sport = _sports.FirstOrDefault(c => c.Id == _selectedSport.SportId);
                        var segment = sport?.Segments.FirstOrDefault(c => c.Id == _selectedSport.SegmentId);
                        var league = segment?.Leagues.FirstOrDefault(c => c.Id == _selectedSport.LeagueId);

                        _selectedSport = new SportClickCallbackModel()
                        {
                            SportId = _selectedSport.SportId,
                            SportName = sport?.Name ?? "-",
                            SegmentId = _selectedSport.SegmentId,
                            SegmentName = segment?.Name ?? "-",
                            LeagueId = _selectedSport.LeagueId,
                            LeagueName = league?.Name ?? "-",

                        };

                        await OnSportClickCallback.InvokeAsync(_selectedSport);
                    }
                }

            }

            _init = false;

            if (_navMenus == null)
            {
                _navMenus = new NavMenuModel()
                {
                    Menus = NavMenuParameter?.NavMenus?.Menus?.Select(x => new MenuModel()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        IconName = x.IconName,
                        Url = x.Url,
                        SubMenus = x.SubMenus
                    }).ToList() ?? new List<MenuModel>()
                };
            }

            if (!_navMenus.Equals(NavMenuParameter?.NavMenus))
            {
                _navMenus = new NavMenuModel()
                {
                    Menus = NavMenuParameter?.NavMenus?.Menus?.Select(x => new MenuModel()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        IconName = x.IconName,
                        Url = x.Url,
                        SubMenus = x.SubMenus
                    }).ToList() ?? new List<MenuModel>()
                };
            }
        }

        private async Task HandleSportClick(SportClickCallbackModel selectedSport)
        {

            var sport = _sports.FirstOrDefault(c => c.Id == _selectedSport.SportId);
            var segment = sport?.Segments.FirstOrDefault(c => c.Id == _selectedSport.SegmentId);
            var league = segment?.Leagues.FirstOrDefault(c => c.Id == _selectedSport.SegmentId);

            _selectedSport = new SportClickCallbackModel()
            {
                SportId = sport?.Id ?? -1,
                SportName = sport?.Name,
                SegmentId = segment?.Id ?? -1,
                SegmentName = segment?.Name,
                LeagueId = league?.Id ?? -1,
                LeagueName = league?.Name
            };
        }
        private async Task HandleSportMenuEvent(int sportId, string sportName)
        {
            var sports = _sports.Where(c => c.IsSelected || c.Id == sportId).ToList();

            sports.ForEach(sport =>
            {
                sport.ShouldRender = false;
                var isSelected = sport.Id == sportId && !sport.IsSelected;

                if (sport.IsSelected != isSelected)
                {
                    sport.ShouldRender = true;
                    sport.IsSelected = isSelected;
                }

                if (!isSelected)
                {
                    var segments = sport.Segments.Where(c => c.IsSelected).ToList();
                    segments.ForEach(segment => segment.IsSelected = false);
                }
            });

            _selectedSport = new SportClickCallbackModel()
            {
                SportId = sportId,
                SportName = sportName
            };
        }
        private async Task HandleSegmentMenuEvent(int sportId, string sportName, int segmentId, string segmentName)
        {
            var sports = _sports.Where(c => c.IsSelected || c.Id == sportId).ToList();

            sports.ForEach(sport =>
            {
                sport.ShouldRender = false;
                var isSelected = sport.Id == sportId;

                if (!isSelected)
                {
                    var segments = sport.Segments.Where(c => c.IsSelected).ToList();
                    segments.ForEach(segment => segment.IsSelected = false);
                }
            });

            _sports.ForEach(sport =>
            {
                sport.ShouldRender = false;
                var isSelected = sport.Id == sportId;

                if (sport.IsSelected != isSelected)
                {
                    sport.ShouldRender = true;
                    sport.IsSelected = isSelected;
                }

                if (sport.Id == sportId)
                    sport.Segments.ForEach(segment =>
                    {
                        segment.ShouldRender = false;

                        var isSelected = segment.Id == segmentId && !segment.IsSelected;

                        if (segment.IsSelected != isSelected)
                        {
                            sport.ShouldRender = true;
                            segment.ShouldRender = true;
                            segment.IsSelected = isSelected;
                        }
                    });
            });

            _selectedSport = new SportClickCallbackModel()
            {
                SportId = sportId,
                SportName = sportName,
                SegmentId = segmentId,
                SegmentName = segmentName
            };
        }
        private async Task HandleLeagueMenuEvent(int sportId, string sportName, int segmentId, string segmentName, int leagueId, string leagueName)
        {
            var isPageChanged = !_monitoringUrls.Contains(_page) || _page.Contains("monitoring-details");

            if (_selectedSport.LeagueId != leagueId || isPageChanged)
            {
                _selectedSport = new SportClickCallbackModel()
                {
                    SportId = sportId,
                    SportName = sportName,
                    SegmentId = segmentId,
                    SegmentName = segmentName,
                    LeagueId = leagueId,
                    LeagueName = leagueName
                };

                var slugs = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "").Split("/").ToList();
                if (slugs.Count != 1 && slugs[1].ToLower() != "monitoring")
                {
                    _navigationManager.NavigateTo($"/hk/odds-engine");
                }

                await OnSportClickCallback.InvokeAsync(_selectedSport);
            }
        }

        private void OnLogoutClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md modal-dialog-centered");
            parameters.Add("ModalHeaderClass", "text-center");
            parameters.Add("TitleClass", "w-100");

            var options = new ModalOptions();
            _modalService.Open<ConfirmLogoutModal>("Logout", parameters, options);
        }

        private void OnChangePasswordClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md modal-dialog-centered");
            parameters.Add("TitleClass", "w-100");

            var options = new ModalOptions();
            _modalService.Open<ChangePasswordModal>("Change Password", parameters, options);
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private async Task<List<SportItemModel>> GetSportTreeMenus(LanguageCode languageCode, CancellationToken token)
        {
            var result = new List<SportItemModel>();

            var response = await _backofficeService.GetSportsTreeAsync((int)FixtureType.Match, languageCode, token);

            if (token.IsCancellationRequested)
                return new();

            if (response.StatusCode == StatusCode.Success)
            {
                result = response.Result.Sports
                    .Select(sp => new SportItemModel
                    {
                        Id = sp.SportId,
                        Name = sp.SportName,
                        SortOrder = sp.SortOrder ?? -1,
                        MenuEvent = async e => await HandleSportMenuEvent(sp.SportId, sp.SportName),
                        Segments = sp.Segments.Select(se => new SegmentItemModel
                        {
                            Id = se.SegmentId,
                            Name = se.SegmentName,
                            SortOrder = se.SortOrder,
                            MenuEvent = async e => await HandleSegmentMenuEvent(sp.SportId, sp.SportName, se.SegmentId, se.SegmentName),
                            Leagues = se.Leagues.Select(le => new LeagueItemModel
                            {
                                Id = le.LeagueId,
                                Name = le.LeagueName,
                                SortOrder = le.SortOrder,
                                MenuEvent = async e => await HandleLeagueMenuEvent(sp.SportId, sp.SportName, se.SegmentId, se.SegmentName, le.LeagueId, le.LeagueName)
                            }).ToList()
                        }).ToList()
                    }).ToList();
            }


            return result;
        }
        private async Task<List<SportModel>> GetSportsTreeMenu(LanguageCode language, CancellationToken token)
        {
            var response = await _backofficeService.GetSportsTreeAsync((int)FixtureType.Match, language, token);
            var result = new List<SportModel>();

            if (token.IsCancellationRequested)
                return new();
            ;
            if (response.StatusCode == StatusCode.Success)
            {
                result = response.Result.Sports
                    .Select(sp => new SportModel
                    {
                        SportId = sp.SportId,
                        SportName = sp.SportName,
                        SortOrder = sp.SortOrder,
                        Segments = sp.Segments.Select(se => new SegmentModel
                        {
                            SegmentId = se.SegmentId,
                            SegmentName = se.SegmentName,
                            SortOrder = se.SortOrder,
                            Leagues = se.Leagues.Select(le => new LeagueModel
                            {
                                LeagueId = le.LeagueId,
                                LeagueName = le.LeagueName,
                                SortOrder = le.SortOrder,
                            })
                            .ToList()
                        })
                        .ToList()
                    })
                    .ToList();
            }
            return result;
        }

        #endregion
    }
}
