using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Monitoring;
using Backoffice.Web.Models.Shared;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;

namespace Backoffice.Web.Shared.OddsEngine
{
    public partial class OEMonitoringLayout
    {
        #region Properties
        [Inject] public new ITradingService TradingService { get; set; }
        [Inject] public new IBackofficeService BackofficeService { get; set; }

        private HeaderNavMenu _headerNavMenuReference;
        private SportClickCallbackModel _selectedSport;
        private NavMenuParameter _navMenuParameter = new();

        private List<SportItemModel> _sports = new();
        private CancellationTokenSource _cts;
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _navMenuParameter.NavMenuAccess = PermissionCategory.OE;

            var ctsToken = _cts.Token;

            var clientResult = (await BackofficeService.GetPlatformClients()).Result;

            if (!ctsToken.IsCancellationRequested)
                UserState.SetTradingClients(clientResult.Clients);

            base.InitializeState();
        }
        private void HandleSportClick(SportClickCallbackModel model)
        {
            _selectedSport = model;
            _headerNavMenuReference.ActivateMenu("monitoring");
        }
        private async Task<List<SportItemModel>> GetSportTreeMenus(CancellationToken token)
        {
            var result = new List<SportItemModel>();

            var response = await _backofficeService.GetSportsTreeAsync((int)FixtureType.Match, _translationState.ActiveLanguageCode, token);

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
        private async Task HandleSportMenuEvent(int sportId, string sportName)
        {
            var sports = _sports.Where(c => c.IsSelected || c.Id == sportId).ToList();

            sports.ForEach(sport =>
            {
                var isSelected = sport.Id == sportId;

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
                var isSelected = sport.Id == sportId;

                if (!isSelected)
                {
                    var segments = sport.Segments.Where(c => c.IsSelected).ToList();
                    segments.ForEach(segment => segment.IsSelected = false);
                }
                else
                {
                    var segments = sport.Segments.Where(c => c.IsSelected || c.Id == segmentId).ToList();
                    segments.ForEach(segment =>
                    {
                        isSelected = segment.Id == segmentId;

                        if (segment.IsSelected != isSelected)
                        {
                            segment.ShouldRender = true;
                            segment.IsSelected = isSelected;
                        }
                    });
                }
            });

            _sports.ForEach(sport =>
            {
                if (sport.ShouldRender)
                    sport.ShouldRender = false;

                var isSelected = sport.Id == sportId;

                if (sport.IsSelected != isSelected)
                {
                    sport.ShouldRender = true;
                    sport.IsSelected = isSelected;
                }

                var segments = sport.Segments.Where(c => c.IsSelected).ToList();
                segments.ForEach(segment => segment.IsSelected = false);

                if (sport.Id == sportId)
                    sport.Segments.ForEach(segment =>
                    {
                        if (segment.ShouldRender)
                            segment.ShouldRender = false;

                        var isSelected = segment.Id == segmentId;

                        if (segment.IsSelected != isSelected)
                        {
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
            if (_selectedSport.LeagueId != leagueId)
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

                //await OnSportClickCallback.InvokeAsync(_selectedSport);

            }
        }
        #endregion
    }
}
