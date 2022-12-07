using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.TopFixtures
{
    public partial class Select : ComponentBase, IDisposable
    {
        #region Properties
        [Parameter] public List<SportsTreeResponse.SportReponse> SportsTree { get; set; }
        [Parameter] public int RowCount { get; set; }
        [Parameter] public EventCallback<EditTopFixturesModel> OnSelectedGame { get; set; }
        #endregion

        #region Methods
        public async void OnGameSelectedEvent(FixtureModel fixture)
        {
            fixture.SortOrder = RowCount;
            var model = new EditTopFixturesModel
            {
                SelectedGame = fixture,
            };
            await OnSelectedGame.InvokeAsync(model);
        }

        public void Dispose()
        {

        }
        #endregion

    }
}
