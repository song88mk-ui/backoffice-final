using Backoffice.Shared.Enums;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;

namespace Backoffice.Web.Shared
{
    public partial class NavMenu : IDisposable
    {
        #region Properties

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        [Parameter] public PermissionCategory NavMenuAccess { get; set; }
        [Parameter] public NavMenuModel? NavMenus { get; set; }
        [Parameter] public EventCallback<SportClickCallbackModel> OnSportClickCallback { get; set; }
        [Parameter] public string? Region { get; set; }

        #endregion

        #region Fields

        private List<Claim> _userClaims = new List<Claim>();

        private PermissionCategory? _navMenuAccess;
        private LanguageCode _languageCode;

        private SportsTreeModel _menu = new SportsTreeModel();
        private SportClickCallbackModel _selectedSport = new SportClickCallbackModel();
        private NavMenuModel _navMenus;

        private string[] _permissionCategories = new string[] { nameof(PermissionCategory.Admin), nameof(PermissionCategory.SBC) };
        private string[] _access = new string[] { ((int)Access.View).ToString(), ((int)Access.Edit).ToString() };

        private string _page;

        private bool _showSwitchAccess;
        private bool collapseNavMenu = true;
        private bool _init = true;

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            _page = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "");

            var splitVal = _page.Split("/").FirstOrDefault();

            if (splitVal != null && splitVal.Any() && Region != null)
            {
                var regions = new List<string>() { "HK", "SA" };
                Region = regions.Contains(splitVal.Trim().ToUpper()) ? splitVal.Trim().ToUpper() : null;
            }

        }

        protected override async Task OnParametersSetAsync()
        {
            if (!_userClaims.SequenceEqual(UserModel.Claims))
            {
                _userClaims = new List<Claim>(UserModel.Claims);

                var categories = UserModel.Claims?.Where(c => _access.Contains(c.Value)).Select(s => s.Type.Split(".")[0]).Distinct().ToList();
                categories = categories?.Where(c => _permissionCategories.Contains(c)).ToList();

                _showSwitchAccess = categories != null && categories.Count > 1;
            }

            if (_init || _navMenuAccess != NavMenuAccess || _languageCode != TranslationState.ActiveLanguageCode)
            {
                _init = false;
                _languageCode = TranslationState.ActiveLanguageCode;
                _navMenuAccess = NavMenuAccess;

                var categories = _userClaims?.Where(c => _access.Contains(c.Value)).Select(s => s.Type.Split(".")[0]).Distinct().ToList();
                categories = categories?.Where(c => _permissionCategories.Contains(c)).ToList();

                _showSwitchAccess = categories != null && categories.Count > 1;

                if (_navMenuAccess == PermissionCategory.OE)
                {
                    _menu.Sports = await GetSportsTreeMenu();
                    var sport = _menu.Sports.FirstOrDefault();

                    if (sport != null)
                    {
                        _selectedSport.SportId = sport.SportId;
                        _selectedSport.SportName = sport.SportName;

                        var segment = sport.Segments.FirstOrDefault();

                        if (segment != null)
                        {
                            _selectedSport.SegmentId = segment.SegmentId;
                            _selectedSport.SegmentName = segment.SegmentName;

                            var league = segment.Leagues.FirstOrDefault();

                            if (league != null)
                            {
                                _selectedSport.LeagueId = league.LeagueId;
                                _selectedSport.LeagueName = league.LeagueName;
                            }
                        }
                    }

                    switch (_page?.ToLower() ?? string.Empty)
                    {
                        //case "sa/odds-engine":
                        case "hk/odds-engine":
                        //case "sa/odds-engine/monitoring":
                        case "hk/odds-engine/monitoring":
                            await OnSportClickCallback.InvokeAsync(_selectedSport);
                            break;
                    }
                }
            }

            if (_navMenus == null)
            {
                _navMenus = new NavMenuModel()
                {
                    Menus = NavMenus?.Menus?.Select(x => new MenuModel()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        IconName = x.IconName,
                        Url = x.Url,
                        SubMenus = x.SubMenus
                    }).ToList() ?? new List<MenuModel>()
                };
            }

            if (!_navMenus.Equals(NavMenus))
            {
                _navMenus = new NavMenuModel()
                {
                    Menus = NavMenus?.Menus?.Select(x => new MenuModel()
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

        private void OnSportClickEvent(int sportId)
        {
            if (_selectedSport.SportId != sportId)
            {
                var sport = _menu.Sports.FirstOrDefault(c => c.SportId == sportId);

                if (sport != null)
                {
                    _selectedSport.SportId = sport.SportId;
                    _selectedSport.SportName = sport.SportName;
                }
            }
            else
            {
                _selectedSport.SportId = 0;
                _selectedSport.SportName = string.Empty;
            }

            // reset selected segment and league if new sport has been selected. 
            _selectedSport.SegmentId = 0;
            _selectedSport.SegmentName = string.Empty;

            _selectedSport.LeagueId = 0;
            _selectedSport.LeagueName = string.Empty;
        }

        private async Task OnSegmentClickEvent(int segmentId)
        {
            if (_selectedSport.SegmentId != segmentId)
            {
                var sport = _menu.Sports.FirstOrDefault(c => c.SportId == _selectedSport.SportId);
                var segment = sport?.Segments.FirstOrDefault(c => c.SegmentId == segmentId);

                if (segment != null)
                {
                    _selectedSport.SegmentId = segment.SegmentId;
                    _selectedSport.SegmentName = segment.SegmentName;
                }
            }
            else
            {
                _selectedSport.SegmentId = 0;
                _selectedSport.SegmentName = string.Empty;
            }
        }

        private async Task OnLeagueClickEvent(int leagueId)
        {
            if (_selectedSport.LeagueId != leagueId)
            {
                var sport = _menu.Sports.FirstOrDefault(c => c.SportId == _selectedSport.SportId);
                var segment = sport?.Segments.FirstOrDefault(c => c.SegmentId == _selectedSport.SegmentId);
                var league = segment?.Leagues.FirstOrDefault(c => c.LeagueId == leagueId);

                _selectedSport.LeagueId = league.LeagueId;
                _selectedSport.LeagueName = league.LeagueName;

                // verify if you're in the monitoring page, if not then the user will be navigated.
                var slugs = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "").Split("/").ToList();

                if (slugs.Count != 1 && slugs[1].ToLower() != "monitoring")
                {
                    _navigationManager.NavigateTo($"/{Region}/odds-engine");
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

        private async Task<List<SportModel>> GetSportsTreeMenu()
        {
            var response = await _backofficeService.GetSportsTreeAsync((int)FixtureType.Match, _languageCode);
            var result = new List<SportModel>();

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
