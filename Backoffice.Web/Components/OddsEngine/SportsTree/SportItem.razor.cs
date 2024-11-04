using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Monitoring;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Backoffice.Web.Components.OddsEngine.SportsTree
{
    public partial class SportItem
    {
        [Parameter] public SportClickCallbackModel SelectecSport { get; set; }
        [Parameter] public SportItemModel Sport { get; set; }
        [Parameter] public EventCallback<SportClickCallbackModel> OnSportClickCallback { get; set; }

        private bool _init = true;
        private bool _shouldRender = true;

        private bool _isSelected;
        private string _cssArrowIcon;

        protected override Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameter();

            if (_shouldRender)
            {
                _isSelected = Sport.IsSelected;

                _cssArrowIcon = _isSelected ? "fa-chevron-up" : "fa-chevron-down";
            }

            return Task.CompletedTask;
        }

        private bool ValidateParameter() => _init || _isSelected != Sport.IsSelected || Sport.ShouldRender;

        private async Task HandleArrowClick()
        {
            _isSelected = !_isSelected;
            Sport.IsSelected = _isSelected;
            var segment = Sport.Segments.FirstOrDefault();
            var league = segment?.Leagues.FirstOrDefault();

            SelectecSport =new SportClickCallbackModel()
            {
                SportId = Sport.Id,
                SegmentId = segment?.Id ?? -1,
                LeagueId = league?.Id ?? -1
            };

            //await OnSportClickCallback.InvokeAsync(SelectecSport);

            _shouldRender = true;
        }
        public async Task HandleSegmentClick(int segmentId)
        {
            foreach (var segment in Sport.Segments.Where(c => c.Id != segmentId && c.IsSelected))
            {
                segment.IsSelected = false;

                foreach (var league in segment.Leagues.Where(c => c.IsSelected))
                {
                    league.IsSelected = false;
                }
            }

            if (SelectecSport.SportId == Sport.Id && SelectecSport.SegmentId == segmentId)
            {
                var segment = Sport.Segments.FirstOrDefault(c => c.Id == SelectecSport.SegmentId);

                if (segment == null || SelectecSport.LeagueId == 0)
                    return;

                var league = segment.Leagues.FirstOrDefault(c => c.Id == SelectecSport.LeagueId);

                if (league != null)
                {
                    league.IsSelected = true;
                }
            }

            _shouldRender = true;
        }
    }
}
