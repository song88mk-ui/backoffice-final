using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text;
using System.Text.Json;
using SportModel = Backoffice.Web.Models.SportModel;

namespace Backoffice.Web.Components.SBC.TopFixtures
{
    public partial class EditModal : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        public IJSStreamReference? DataReference { get; set; }

        public ClientTopFixtureModel.OrderedClientTopFixture TopFixture { get; set; }
        public ClientTopFixtureModel.OrderedClientTopFixture NewTopFixture { get; set; }
        public List<SportModel> SportsTree { get; set; }
        public List<string> CurrentTopFixtures { get; set; }

        public int SelectedRow { get; set; }
        public int CurrentIndex { get; set; }
        public string TableClass { get; set; }
        public bool IsDisabled { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SportsTree = new List<SportModel>();
            TableClass = "table-edit-top-fixture";

            TopFixture = ModalParameters.TryGet<ClientTopFixtureModel.OrderedClientTopFixture>("TopFixture");

            CurrentTopFixtures = TopFixture.TopFixtures.Any() ? TopFixture.TopFixtures.Select(s => s.FixtureId).ToList() : new List<string>();

            IsDisabled = CurrentTopFixtures.Count() >= 50;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JSRuntime.InvokeVoidAsync("initSort", $".{TableClass}", ".drag-handle");
            }
        }

        public async Task OnInitEvent(EditTopFixturesModel model)
        {
            NewTopFixture = new ClientTopFixtureModel.OrderedClientTopFixture();

            if (TopFixture.TopFixtures.Any())
            {
                var rowCount = 1;

                foreach (var topFixture in TopFixture.TopFixtures)
                {
                    if ((model.SelectedSport.SportId == 0 || model.SelectedSport.SportId == topFixture.SportId)
                        && (model.SelectedSegment.SegmentId == 0 || model.SelectedSegment.SegmentId == topFixture.SegmentId)
                        && (model.SelectedLeague.LeagueId == 0 || model.SelectedLeague.LeagueId == topFixture.LeagueId))
                    {
                        if (string.IsNullOrEmpty(model.SelectedGameFilter.FixtureId) || model.SelectedGameFilter.FixtureId == "0" || model.SelectedGameFilter.FixtureId == topFixture.FixtureId)
                        {
                            topFixture.SortOrder = rowCount;

                            NewTopFixture.TopFixtures.Add(topFixture);

                            rowCount++;
                        }
                    }
                }
            }

            if (!NewTopFixture.TopFixtures.Any())
            {
                NewTopFixture.TopFixtures.Add(new ClientTopFixtureModel.TopFixture
                {
                    SortOrder = 1
                });
            }

            //NewTopFixture.TopFixtures = NewTopFixture.TopFixtures.OrderByDescending(o => o.FixtureStartDate).ToList();

            if (model.SortFilter == "2")
                NewTopFixture.TopFixtures = NewTopFixture.TopFixtures.OrderBy(t => t.FixtureStartDate).ToList();

            SportsTree = model.Sports;

            _ = SortOrderRow();
        }

        public async Task OnDropdownSelect(int index)
        {
            SelectedRow = index;
        }

        public async Task OnSelectedGameEvent(FixtureModel model)
        {
            foreach (var fixture in NewTopFixture.TopFixtures)
            {
                if (model.SortOrder == fixture.SortOrder)
                {
                    fixture.SportId = model.SportId;
                    fixture.SportName = model.SportName;
                    fixture.SegmentId = model.SegmentId;
                    fixture.SegmentName = model.SegmentName;
                    fixture.LeagueId = model.LeagueId;
                    fixture.LeagueName = model.LeagueName;
                    fixture.FixtureStartDate = model.StartDate;
                    fixture.FixtureId = model.FixtureId;
                    fixture.FixtureName = model.FixtureName;
                    fixture.FixtureStatus = model.FixtureStatus;
                    fixture.SortOrder = model.SortOrder;

                    break;
                }
            }

            CurrentTopFixtures = NewTopFixture.TopFixtures.Any() ?
                NewTopFixture.TopFixtures.Select(s => s.FixtureId).ToList() : new List<string>();
        }

        public async Task OnAddClickEvent(int count)
        {
            NewTopFixture.TopFixtures.Insert(count, new ClientTopFixtureModel.TopFixture
            {
                SortOrder = count + 1
            });

            IsDisabled = NewTopFixture.TopFixtures.Any() && NewTopFixture.TopFixtures.Count >= 50 ? true : false;

            _ = SortOrderRow();
        }

        public async Task OnDeleteRow(ClientTopFixtureModel.TopFixture fixture)
        {
            NewTopFixture.TopFixtures.Remove(fixture);

            if (NewTopFixture.TopFixtures.Any() && NewTopFixture.TopFixtures.Count < 50)
            {
                IsDisabled = false;
            }

            if (NewTopFixture.TopFixtures.Count == 0)
                NewTopFixture.TopFixtures.Add(new ClientTopFixtureModel.TopFixture() { SortOrder = 1 });

            await SortOrderRow();

        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnSaveClickEvent()
        {
            if (NewTopFixture.TopFixtures == null || !NewTopFixture.TopFixtures.Any())
                return;

            var topFixtures = (await GetSortOrders()).Where(c => !string.IsNullOrEmpty(c.RowId))
                .Select(s => new ClientTopFixtureSetup
                {
                    FixtureId = long.Parse(s.RowId.Split("-")[1]),
                    SortOrder = s.SortOrder
                }).ToList();

            await BackofficeService.PostClientTopFixtures(new PostClientTopFixtureRequest()
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

        private async Task<List<SortOrderModel>> GetSortOrders()
        {
            var rows = new List<SortOrderModel>();

            DataReference = await JSRuntime.InvokeAsync<IJSStreamReference>("getSortOrder", $".{TableClass}");

            using (var stream = await DataReference.OpenReadStreamAsync(maxAllowedSize: 10_000_000))
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    var hasLine = true;

                    while (hasLine)
                    {
                        var content = await reader.ReadLineAsync();

                        if (content == null)
                        {
                            hasLine = false;
                            continue;
                        }

                        rows.Add(JsonSerializer.Deserialize<SortOrderModel>(content));
                    }
                }
            }

            return rows;
        }

        private async Task SortOrderRow()
        {
            NewTopFixture.TopFixtures = NewTopFixture.TopFixtures.Select((s, index) =>
            {
                s.SortOrder = index + 1;

                return s;
            }).ToList();
        }

        #endregion
    }
}
