using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.TopFixtures
{
    public partial class Select : ComponentBase, IDisposable
    {
        #region Properties

        [Parameter] public List<SportsTreeModel.SportModel> SportsTree { get; set; }
        [Parameter] public List<string> CurrentTopFixtures { get; set; }
        [Parameter] public string SelectedTopFixture { get; set; }
        [Parameter] public int RowCount { get; set; }
        [Parameter] public EventCallback<FixtureModel> OnSelectedGame { get; set; }

        #endregion

        #region Methods

        public async void OnGameSelectedEvent(FixtureModel fixture)
        {
            fixture.SortOrder = RowCount;

            await OnSelectedGame.InvokeAsync(fixture);
        }

        public void Dispose()
        {

        }

        #endregion

    }
}
