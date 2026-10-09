using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.LeagueSorting
{
    public partial class LeagueSelect
    {
        private const int MinFilterLength = 2;

        [Parameter] public List<SegmentModel> Segments { get; set; } = new List<SegmentModel>();
        // (segment id, league id) pairs already used by the other rows, which are not offered again.
        [Parameter] public HashSet<(int SegmentId, int LeagueId)> Taken { get; set; } = new HashSet<(int, int)>();
        [Parameter] public int SelectedSegmentId { get; set; }
        [Parameter] public int SelectedLeagueId { get; set; }
        [Parameter] public EventCallback<(SegmentModel Segment, LeagueModel League)> OnSelected { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }

        private readonly HashSet<int> _expanded = new HashSet<int>();
        private string _filter = string.Empty;

        private bool IsFiltering => (_filter?.Trim().Length ?? 0) >= MinFilterLength;

        private IEnumerable<SegmentModel> VisibleSegments => Segments.Where(s => AvailableLeagues(s).Any());

        private IEnumerable<LeagueModel> AvailableLeagues(SegmentModel segment)
        {
            var leagues = segment.Leagues.Where(l => !Taken.Contains((segment.SegmentId, l.LeagueId)));

            if (!IsFiltering)
                return leagues;

            var term = _filter.Trim();

            return segment.SegmentName != null && segment.SegmentName.Contains(term, StringComparison.OrdinalIgnoreCase)
                ? leagues
                : leagues.Where(l => l.LeagueName != null && l.LeagueName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        // While searching, every segment that still has a match is opened so results are visible at once.
        private bool IsExpanded(SegmentModel segment) =>
            IsFiltering || _expanded.Contains(segment.SegmentId) || segment.SegmentId == SelectedSegmentId && SelectedLeagueId != 0;

        private bool IsSelected(SegmentModel segment, LeagueModel league) =>
            segment.SegmentId == SelectedSegmentId && league.LeagueId == SelectedLeagueId;

        private void ToggleSegment(int segmentId)
        {
            if (!_expanded.Remove(segmentId))
                _expanded.Add(segmentId);
        }

        private async Task OnLeagueClick(SegmentModel segment, LeagueModel league) =>
            await OnSelected.InvokeAsync((segment, league));
    }
}
