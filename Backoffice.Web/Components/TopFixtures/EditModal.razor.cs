using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.TopFixtures
{
    public partial class EditModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        public ClientTopFixtureResponse.OrderedClientTopFixture TopFixture { get; set; }

        public ClientTopFixtureResponse.OrderedClientTopFixture NewTopFixture { get; set; }
        public List<SportsTreeResponse.SportReponse> SportsTree { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            try
            {
                TopFixture = ModalParameters.TryGet<ClientTopFixtureResponse.OrderedClientTopFixture>("TopFixture");
                SportsTree = new List<SportsTreeResponse.SportReponse>();
            }
            catch (Exception)
            {

            }
        }

        public async Task OnInitEvent(EditTopFixturesModel model)
        {
            NewTopFixture = new ClientTopFixtureResponse.OrderedClientTopFixture();
            if (TopFixture.TopFixtures.Any())
            {
                NewTopFixture.TopFixtures = new List<FixtureModel>();
                NewTopFixture.TopFixtures.AddRange(TopFixture.TopFixtures);
            }

            SportsTree = model.Sports;
        }

        public async Task OnSelectedGameEvent(EditTopFixturesModel model)
        {
            if(model.RowCtr <= 1)
            {
                NewTopFixture.TopFixtures = new List<FixtureModel>();
                NewTopFixture.TopFixtures.Add(model.SelectedGame);
            }
            else
            {
                var rowCtr = 0;
                foreach(var fixture in NewTopFixture.TopFixtures)
                {
                    rowCtr = rowCtr + 1;
                    if(rowCtr == model.RowCtr)
                    {
                        fixture.SportId = model.SelectedGame.SportId;
                        fixture.SportName = model.SelectedGame.SportName;
                        fixture.SegmentId = model.SelectedGame.SegmentId;
                        fixture.SegmentName = model.SelectedGame.SegmentName;
                        fixture.LeagueId = model.SelectedGame.LeagueId;
                        fixture.LeagueName = model.SelectedGame.LeagueName;
                        fixture.StartDate = model.SelectedGame.StartDate;
                        fixture.FixtureId = model.SelectedGame.FixtureId;
                        fixture.FixtureName = model.SelectedGame.FixtureName;
                        fixture.FixtureStatus = model.SelectedGame.FixtureStatus;
                        fixture.SortOrder = model.SelectedGame.SortOrder;
                        break;
                    }
                }

            }
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnSaveClickEvent()
        {

        }

        public void Dispose()
        {

        }
        #endregion
    }
}
