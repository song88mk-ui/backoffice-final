using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Components.TopFixtures
{
    public partial class EditModal : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        public ClientTopFixtureModel.OrderedClientTopFixture TopFixture { get; set; }
        public ClientTopFixtureModel.OrderedClientTopFixture NewTopFixture { get; set; }
        public List<SportsTreeModel.SportModel> SportsTree { get; set; }
        public List<string> CurrentTopFixtures { get; set; }

        public int SelectedRowIndex { get; set; }
        public int CurrentIndex { get; set; }
        public string TableClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SportsTree = new List<SportsTreeModel.SportModel>();
            TableClass = "table-edit-top-fixture";

            TopFixture = ModalParameters.TryGet<ClientTopFixtureModel.OrderedClientTopFixture>("TopFixture");

            CurrentTopFixtures = TopFixture.TopFixtures.Any() ? TopFixture.TopFixtures.Select(s => s.FixtureId).ToList() : new List<string>();

            SelectedRowIndex = -1;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // "tr > td.actions > a.drag-handle"
                await JSRuntime.InvokeVoidAsync("initSort", $".{TableClass}", ".drag-handle");
            }
        }

        public async Task OnInitEvent(EditTopFixturesModel model)
        {
            NewTopFixture = new ClientTopFixtureModel.OrderedClientTopFixture();

            if (TopFixture.TopFixtures.Any())
            {
                NewTopFixture.TopFixtures = new List<ClientTopFixtureModel.TopFixture>();

                var rowCount = 0;

                foreach (var topFixture in TopFixture.TopFixtures)
                {
                    if (model.SelectedSport.SportId == 0 || model.SelectedSport.SportId == topFixture.SportId)
                    {
                        if (model.SelectedSegment.SegmentId == 0 || model.SelectedSegment.SegmentId == topFixture.SegmentId)
                        {
                            if (model.SelectedLeague.LeagueId == 0 || model.SelectedLeague.LeagueId == topFixture.LeagueId)
                            {
                                if (string.IsNullOrEmpty(model.SelectedGameFilter.FixtureId) || model.SelectedGameFilter.FixtureId == "0" || model.SelectedGameFilter.FixtureId == topFixture.FixtureId)
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

            if (NewTopFixture.TopFixtures == null || !NewTopFixture.TopFixtures.Any())
            {
                NewTopFixture.TopFixtures = new List<ClientTopFixtureModel.TopFixture>();
                NewTopFixture.TopFixtures.Add(new ClientTopFixtureModel.TopFixture() { SortOrder = 1 });
            }

            NewTopFixture.TopFixtures = NewTopFixture.TopFixtures.OrderByDescending(t => t.FixtureStartDate).ToList();
            if (model.SortFilter == "2")
            {
                NewTopFixture.TopFixtures = NewTopFixture.TopFixtures.OrderBy(t => t.FixtureStartDate).ToList();
            }

            SportsTree = model.Sports;

            await SortOrderRow();
        }

        public async Task OnDropdownSelect(ClientTopFixtureModel.TopFixture fixture)
        {
            SelectedRowIndex = NewTopFixture.TopFixtures.FindIndex(c => c == fixture);
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
                    fixture.FixtureStartDate = model.SelectedGame.StartDate;
                    fixture.FixtureId = model.SelectedGame.FixtureId;
                    fixture.FixtureName = model.SelectedGame.FixtureName;
                    fixture.FixtureStatus = model.SelectedGame.FixtureStatus;
                    fixture.SortOrder = model.SelectedGame.SortOrder;
                    break;
                }
            }

            CurrentTopFixtures = NewTopFixture.TopFixtures.Any() ?
                NewTopFixture.TopFixtures.Select(s => s.FixtureId).ToList() : new List<string>();
        }

        public async Task OnAddRow(int rowCount)
        {
            NewTopFixture.TopFixtures.Insert(rowCount, new ClientTopFixtureModel.TopFixture 
            { 
                SortOrder = rowCount + 1 
            });

            await SortOrderRow();
        }

        public async Task OnDeleteRow(ClientTopFixtureModel.TopFixture fixture)
        {
            NewTopFixture.TopFixtures.Remove(fixture);

            if (NewTopFixture.TopFixtures.Count == 0)
                NewTopFixture.TopFixtures.Add(new ClientTopFixtureModel.TopFixture() { SortOrder = 1 });

            await SortOrderRow();

        }

        private async Task SortOrderRow()
        {
            if (NewTopFixture.TopFixtures != null && NewTopFixture.TopFixtures.Any())
            {
                NewTopFixture.TopFixtures = NewTopFixture.TopFixtures.Select((s, index) =>
                {
                    s.Index = index;
                    s.SortOrder = index + 1;

                    return s;
                }).ToList();

                //int rowCtr = 0;
                //foreach (var fixture in NewTopFixture.TopFixtures)
                //{
                //    rowCtr = rowCtr + 1;
                //    fixture.SortOrder = rowCtr;
                //}
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

            await BackOfficeService.PostClientTopFixtures(new PostClientTopFixtureRequest()
            {
                ClientId = TopFixture.ClientIdRef,
                TopFixtures = topFixtures
            });

            ModalService.Cancel();

            await Modal.OnSaveClickCallback.InvokeAsync(TopFixture.ClientIdRef.ToString());
        }

        public void Dispose()
        {

        }

        #endregion

        #region Utilities

        private int GetIndex(string id)
        {
            return NewTopFixture.TopFixtures.FindIndex(c => c.FixtureId == id);
        }

        #endregion
    }
}
