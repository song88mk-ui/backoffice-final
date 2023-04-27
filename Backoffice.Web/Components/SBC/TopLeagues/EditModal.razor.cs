using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text;
using System.Text.Json;
using static Backoffice.Shared.Models.Requests.GetTopLeaguesRequest;

namespace Backoffice.Web.Components.SBC.TopLeagues
{
    public partial class EditModal : IAsyncDisposable, IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        public IJSStreamReference? DataReference { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        [Parameter] public int ClientId { get; set; }
        [Parameter] public int SportId { get; set; }
        [Parameter] public string SportName { get; set; }

        public List<SportsTreeModel.SportModel> SportsTree { get; set; }
        public GetAllLeaguesRequest GetAllLeaguesRequest { get; set; }
        public List<TopLeaguesModel.TopLeague> CurrentTopLeagues { get; set; }
        public List<TopLeaguesModel.TopLeague> NewTopLeagues { get; set; }
        public PostTopLeaguesRequest PostTopLeaguesRequest { get; set; }

        public string TableClass { get; set; }
        public int SelectedRow { get; set; }
        public string AlertMessage { get; set; }
        public AlertStatus AlertStatus { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            TableClass = "table-edit-top-leagues";
            ClientId = ModalParameters.TryGet<int>("ClientId");
            SportId = !string.IsNullOrWhiteSpace(ModalParameters.TryGet<string>("SportId")) ? int.Parse(ModalParameters.TryGet<string>("SportId")) : 1;
            SportName = !string.IsNullOrWhiteSpace(ModalParameters.TryGet<string>("SportName")) ? ModalParameters.TryGet<string>("SportName") : "Soccer";

            CurrentTopLeagues = ModalParameters.TryGet<List<TopLeaguesModel.TopLeague>>("CurrentTopLeagues");
            AlertStatus = AlertStatus.Success;
            //SelectedRow = -1;

            GetAllLeaguesRequest = new GetAllLeaguesRequest()
            {
                ClientIdRef = ClientId,
                SportId = SportId,
                SegmentId = 0,
                LeagueId = 0,
                LanguageCode = "en"
            };
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JSRuntime.InvokeVoidAsync("initSort", $".{TableClass}", ".drag-handle");
            }
        }

        public async Task OnInitCallback(EditTopLeaguesModel model)
        {
            NewTopLeagues = new List<TopLeaguesModel.TopLeague>();

            if (CurrentTopLeagues.Any())
            {
                if (model.SelectedSegment != null && model.SelectedSegment?.SegmentId != 0)
                {
                    NewTopLeagues = CurrentTopLeagues.Where(c => c.SegmentId == model.SelectedSegment?.SegmentId).ToList();

                    if (model.SelectedLeague != null && !string.IsNullOrWhiteSpace(model.SelectedLeague?.LeagueId.ToString()) && model.SelectedLeague?.LeagueId.ToString() != "0")
                        NewTopLeagues = CurrentTopLeagues.Where(c => c.SegmentId == model.SelectedSegment?.SegmentId && c.LeagueId == model.SelectedLeague?.LeagueId).ToList();
                }
                else
                    NewTopLeagues = CurrentTopLeagues;
            }

            if (NewTopLeagues == null || !NewTopLeagues.Any())
            {
                NewTopLeagues = new List<TopLeaguesModel.TopLeague>
                {
                    new TopLeaguesModel.TopLeague 
                    { 
                        SortOrder = 1 
                    }
                };
            }

            SportsTree = model.Sports;
        }

        public async Task OnDropdownClick(int count)
        {
            if (SelectedRow != count)
            {
                SelectedRow = count;
            }
            else
            {
                SelectedRow = 0;
            }
        }

        public async Task OnSelectedLeagueEvent(TopLeaguesModel.TopLeague model)
        {
            foreach (var league in NewTopLeagues)
            {
                if (model.SortOrder == league.SortOrder)
                {
                    league.SportId = model.SportId;
                    league.SportName = model.SportName;
                    league.SegmentId = model.SegmentId;
                    league.SegmentName = model.SegmentName;
                    league.LeagueId = model.LeagueId;
                    league.LeagueName = model.LeagueName;
                    league.SortOrder = model.SortOrder;

                    break;
                }
            }

            CurrentTopLeagues = NewTopLeagues.Any() ? NewTopLeagues.ToList() : new List<TopLeaguesModel.TopLeague>();
            SelectedRow = 0;
        }

        public async Task OnAddClickEvent(int count)
        {
            if (NewTopLeagues.Count < 50)
            {
                NewTopLeagues.Insert(count, new TopLeaguesModel.TopLeague
                {
                    SortOrder = count + 1
                });

                SortOrderRow();
            }
        }

        public async Task OnDeleteClickEvent(TopLeaguesModel.TopLeague model)
        {
            NewTopLeagues.Remove(model);

            if (!NewTopLeagues.Any())
            {
                NewTopLeagues.Add(new TopLeaguesModel.TopLeague 
                { 
                    SortOrder = 1 
                });
            }
            else
                SortOrderRow();
        }

        private async Task SortOrderRow()
        {
            NewTopLeagues = NewTopLeagues
                .Select((s, index) => new TopLeaguesModel.TopLeague
                {
                    SportId = s.SportId,
                    SportName = s.SportName,
                    SegmentId = s.SegmentId,
                    SegmentName = s.SegmentName,
                    LeagueId = s.LeagueId,
                    LeagueName = s.LeagueName,
                    SortOrder = index + 1,
                    Status = s.Status,
                    LiveCount = s.LiveCount
                })
                .OrderBy(o => o.SortOrder)
                .ToList();
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnSaveClickEvent()
        {
            AlertMessage = string.Empty;

            //validate
            if (NewTopLeagues.Count == 1 && NewTopLeagues.Any(x => string.IsNullOrEmpty(x.SegmentName) || string.IsNullOrEmpty(x.LeagueName)))
            {
                AlertMessage = "Select atleast 1 League";
                AlertStatus = AlertStatus.Error;

                return;
            }

            if (NewTopLeagues.Count > 1 && NewTopLeagues.Any(x => string.IsNullOrEmpty(x.SegmentName) || string.IsNullOrEmpty(x.LeagueName)))
            {
                AlertMessage = "Please remove empty row";
                AlertStatus = AlertStatus.Error;

                return;
            }            

            if (NewTopLeagues.Count == 1 && NewTopLeagues.Any(c => c.LeagueId == 0))
                NewTopLeagues = new List<TopLeaguesModel.TopLeague>();
            else
                NewTopLeagues.ForEach(c => c.SportName = SportName);

            var topLeagues = (await GetSortOrders()).Select(s => new
            {
                SportId = int.Parse(s.RowId.Split("-")[1]),
                SegmentId = int.Parse(s.RowId.Split("-")[2]),
                LeagueId = int.Parse(s.RowId.Split("-")[3]),
                SortOrder = s.SortOrder
            });

            PostTopLeaguesRequest = new PostTopLeaguesRequest()
            {
                ClientIdRef = ClientId,
                SportId = SportId,
                TopLeagues = NewTopLeagues.Join(topLeagues,
                le => new { le.SportId, le.SegmentId, le.LeagueId },
                so => new { so.SportId, so.SegmentId, so.LeagueId },
                (le, so) => new OrderedClientTopLeague
                {
                    SportId = le.SportId,
                    SportName = le.SportName,
                    SegmentId = le.SegmentId,
                    SegmentName = le.SegmentName,
                    LeagueId = le.LeagueId,
                    LeagueName = le.LeagueName,
                    Status = le.Status,
                    LiveCount = le.LiveCount,
                    SortOrder = so.SortOrder
                }).ToList()
            };

            var response = await BackOfficeService.PostTopLeaguesAsync(PostTopLeaguesRequest);

            if (response.StatusCode == StatusCode.Success)
            {
                await Modal.OnSaveClickCallback.InvokeAsync();
                await Task.Delay(1000);

                AlertMessage = "Top League Saved!";
                AlertStatus = AlertStatus.Success;

                await InvokeAsync(StateHasChanged);
                await Task.Delay(2000);

                ModalService.Cancel();
            }
            else
            {
                AlertMessage = "Error when saving top leagues.";
                AlertStatus = AlertStatus.Error;
            }

        }

        public async ValueTask DisposeAsync()
        {

            if (DataReference is not null)
                await DataReference.DisposeAsync();

            DataReference = null;

            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
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

        #endregion
    }
}
