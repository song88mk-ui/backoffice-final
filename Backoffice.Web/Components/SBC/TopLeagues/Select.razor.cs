using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using static Backoffice.Web.Models.SportsTreeModel;

namespace Backoffice.Web.Components.SBC.TopLeagues
{
    public partial class Select : ComponentBase, IDisposable
    {
        #region Properties

        [Parameter] public List<SportModel> SportsTree { get; set; }
        [Parameter] public List<TopLeaguesModel.TopLeague> CurrentTopLeagues { get; set; }
        [Parameter] public int SelectedTopLeagues { get; set; }
        [Parameter] public int RowCount { get; set; }
        [Parameter] public int SportId { get; set; }
        [Parameter] public EventCallback<TopLeaguesModel.TopLeague> OnSelectedLeague { get; set; }

        #endregion

        #region Methods

        public async void OnSelectedLeagueEvent(LeagueModel league, SegmentModel segmentResponse)
        {
            var model = new TopLeaguesModel.TopLeague
            {
                SegmentId = segmentResponse.SegmentId,
                SegmentName = segmentResponse.SegmentName,
                LeagueId = league.LeagueId,
                LeagueName= league.LeagueName,
                //LiveCount = league.LiveCount,
                SortOrder = RowCount,
                SportId = SportId,
                //SportName = league.SportName,
                //Status = league.Status
            };

            await OnSelectedLeague.InvokeAsync(model);
        }

        public void Dispose()
        {

        }

        #endregion

    }
}
