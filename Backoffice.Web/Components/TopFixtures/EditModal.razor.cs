using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.TopFixtures
{
    public partial class EditModal : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        public ClientTopFixtureResponse.OrderedClientTopFixture TopFixture { get; set; }

        public ClientTopFixtureResponse.OrderedClientTopFixture NewTopFixture { get; set; }
        public List<SportsTreeResponse.SportReponse> SportsTree { get; set; }
        public int SelectedRowIndex { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            try
            {
                TopFixture = ModalParameters.TryGet<ClientTopFixtureResponse.OrderedClientTopFixture>("TopFixture");
                SportsTree = new List<SportsTreeResponse.SportReponse>();
                SelectedRowIndex = -1;
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
                var rowCount = 0;
                foreach (var topFixture in TopFixture.TopFixtures)
                {
                    if(model.SelectedSport.SportId == 0 || model.SelectedSport.SportId == topFixture.SportId)
                    {
                        if(model.SelectedSegment.SegmentId == 0 || model.SelectedSegment.SegmentId == topFixture.SegmentId)
                        {
                            if (model.SelectedLeague.LeagueId == 0 || model.SelectedLeague.LeagueId == topFixture.LeagueId)
                            {
                                if(string.IsNullOrEmpty(model.SelectedGameFilter.FixtureId) || model.SelectedGameFilter.FixtureId == "0" || model.SelectedGameFilter.FixtureId == topFixture.FixtureId)
                                {
                                    rowCount = rowCount + 1;
                                    topFixture.SortOrder = rowCount;
                                    NewTopFixture.TopFixtures.Add(topFixture);
                                }

                            }
                        }
                    }
                }
            }

            if(NewTopFixture.TopFixtures == null || !NewTopFixture.TopFixtures.Any())
            {
                NewTopFixture.TopFixtures = new List<FixtureModel>();
                NewTopFixture.TopFixtures.Add(new FixtureModel() { SortOrder = 1 });
            }

            NewTopFixture.TopFixtures = NewTopFixture.TopFixtures.OrderByDescending(t => t.StartDate).ToList();
            if (model.SortFilter == "2")
            {
                NewTopFixture.TopFixtures = NewTopFixture.TopFixtures.OrderBy(t => t.StartDate).ToList();
            }

            SportsTree = model.Sports;

            await SortOrderRow();
        }

        public async Task OnDropdownSelect(int index)
        {
            SelectedRowIndex = index;
        }

        public async Task OnSelectedGameEvent(EditTopFixturesModel model)
        {
            foreach (var fixture in NewTopFixture.TopFixtures)
            {
                if (model.SelectedGame.SortOrder == fixture.SortOrder)
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

        public async Task OnAddRow(int rowCtr)
        {
            NewTopFixture.TopFixtures.Insert(rowCtr, new FixtureModel() { SortOrder = (rowCtr + 1) });
            await SortOrderRow();
        }
        public async Task OnDeleteRow(FixtureModel fixture)
        {
            NewTopFixture.TopFixtures.Remove(fixture);

            if (NewTopFixture.TopFixtures.Count == 0)
                NewTopFixture.TopFixtures.Add(new FixtureModel() { SortOrder = 1 });

            await SortOrderRow();

        }

        private async Task SortOrderRow()
        {
            if(NewTopFixture.TopFixtures != null && NewTopFixture.TopFixtures.Any())
            {
                int rowCtr = 0;
                foreach (var fixture in NewTopFixture.TopFixtures)
                {
                    rowCtr = rowCtr + 1;
                    fixture.SortOrder = rowCtr;
                }
            }

            await InvokeAsync(StateHasChanged);
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnSaveClickEvent()
        {
            var topFixtures = new List<ClientTopFixtureSetup>();

            if (NewTopFixture.TopFixtures != null && NewTopFixture.TopFixtures.Any())
            {
                int rowCtr = 0;
                foreach (var fixture in NewTopFixture.TopFixtures)
                {
                    if (string.IsNullOrEmpty(fixture.FixtureId))
                        continue;

                    rowCtr = rowCtr + 1;
                    topFixtures.Add(new ClientTopFixtureSetup
                    {
                        FixtureId = Convert.ToInt64(fixture.FixtureId),
                        SortOrder = rowCtr
                    });
                }
            }

            if (!topFixtures.Any())
                return;

            var request = new PostClientTopFixtureRequest()
            {
                ClientId = TopFixture.ClientIdRef,
                TopFixtures = topFixtures
            };
            await BackOfficeService.PostClientTopFixtures(request);

            ModalService.Cancel();

            await Modal.OnSaveClickCallback.InvokeAsync(TopFixture.ClientIdRef);
        }

        public void Dispose()
        {

        }
        #endregion
    }
}
