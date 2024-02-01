using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class EditSegmentLeagues
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Parameter] public List<SportSettingSport> SportSettings { get; set; }
        [Parameter] public EventCallback<List<SportSettingSport>> SportSettingsChanged { get; set; }

        private List<SportSettingSport> _sportSettings;

        private bool _shouldRender = true;

        protected override bool ShouldRender() => _shouldRender;

        protected override Task OnInitializedAsync()
        {
            _sportSettings = SportSettings;

            return base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JSRuntime.InvokeVoidAsync("loadEventListener", DotNetObjectReference.Create(this));
            }
        }

        [JSInvokable]
        public async Task UpdateSportSetting(int sportId, int segmentId, List<SortOrderModel> items)
        {
            var sport = _sportSettings.FirstOrDefault(c => c.SportId == sportId);

            if (segmentId != 0)
            {
                var segment = sport.Segments.FirstOrDefault(c => c.SegmentId == segmentId);

                segment.Leagues = segment.Leagues.Join(items,
                    league => league.LeagueId,
                    sort => int.Parse(sort.RowId),
                    (league, sort) => new SportSettingLeague
                    {
                        SportId = league.SegmentId,
                        SegmentId = league.SegmentId,
                        LeagueId = league.LeagueId,
                        LeagueName = league.LeagueName,
                        SortOrder = sort.SortOrder,
                        IsActive = true
                    })
                    .OrderBy(o => o.SortOrder)
                    .ToList();
            }
            else
            {
                sport.Segments = sport.Segments.Join(items,
                    segment => segment.SegmentId,
                    sort => int.Parse(sort.RowId),
                    (segment, sort) => new SportSettingSegment
                    {
                        SportId = segment.SegmentId,
                        SegmentId = segment.SegmentId,
                        SegmentName = segment.SegmentName,
                        SortOrder = sort.SortOrder,
                        IsActive = true,
                        Leagues = segment.Leagues
                    })
                    .OrderBy(o => o.SortOrder)
                    .ToList();
            }

            SportSettings = _sportSettings;
            await SportSettingsChanged.InvokeAsync(SportSettings);
        }
    }
}
