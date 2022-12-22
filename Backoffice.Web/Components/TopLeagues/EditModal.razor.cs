using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Requests.GetTopLeaguesRequest;

namespace Backoffice.Web.Components.TopLeagues
{
    public partial class EditModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [Parameter] public int ClientId { get; set; }
        [Parameter] public int SportId { get; set; }
        [Parameter] public string SportName { get; set; }
        public List<SportsTreeModel.SportModel> SportsTree { get; set; }

        public GetAllLeaguesRequest GetAllLeaguesRequest { get; set; }
        public List<OrderedClientTopLeague> CurrentTopLeagues { get; set; }
        public List<OrderedClientTopLeague> NewTopLeagues { get; set; }
        public PostTopLeaguesRequest PostTopLeaguesRequest { get; set; }
        public int SelectedRowIndex { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            try
            {
                ClientId = ModalParameters.TryGet<int>("ClientId");
                var sportId = ModalParameters.TryGet<string>("SportId");
                SportId = !string.IsNullOrWhiteSpace(sportId) ? int.Parse(sportId) : 1;
                SportName = !string.IsNullOrWhiteSpace(ModalParameters.TryGet<string>("SportName")) ? ModalParameters.TryGet<string>("SportName") : "Soccer";
                CurrentTopLeagues = ModalParameters.TryGet<List<OrderedClientTopLeague>>("CurrentTopLeagues");
                SelectedRowIndex = -1;

                GetAllLeaguesRequest = new GetAllLeaguesRequest()
                {
                    ClientIdRef = ClientId,
                    SportId = SportId,
                    SegmentId = 0,
                    LeagueId = 0,
                    LanguageCode = "en"
                };
            }
            catch (Exception)
            {
            }
        }

        public async Task OnInitCallback(EditTopLeaguesModel model)
        {
            NewTopLeagues = new List<OrderedClientTopLeague>();
            if (CurrentTopLeagues.Any())
            {
                if (model.SelectedSegment != null && model.SelectedSegment?.SegmentId != 0)
                {
                    NewTopLeagues = CurrentTopLeagues.Where(x => x.SegmentId == model.SelectedSegment?.SegmentId).ToList();

                    if (model.SelectedLeague != null && !string.IsNullOrWhiteSpace(model.SelectedLeague?.LeagueId.ToString()) && model.SelectedLeague?.LeagueId.ToString() != "0")
                        NewTopLeagues = CurrentTopLeagues.Where(x => x.SegmentId == model.SelectedSegment?.SegmentId && x.LeagueId == model.SelectedLeague?.LeagueId).ToList();
                }
                else
                    NewTopLeagues = CurrentTopLeagues;
            }

            if (NewTopLeagues == null || !NewTopLeagues.Any())
            {
                NewTopLeagues = new List<OrderedClientTopLeague>
                {
                    new OrderedClientTopLeague() { SortOrder = 1 }
                };
            }

            SportsTree = model.Sports;

            await SortOrderRow();
        }

        public async Task OnDropdownSelect(int index)
        {
            SelectedRowIndex = index;
        }

        public async Task OnSelectedLeagueEvent(OrderedClientTopLeague model)
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

            CurrentTopLeagues = NewTopLeagues.Any() ? NewTopLeagues.ToList() : new List<OrderedClientTopLeague>();
        }

        public async Task OnAddRow(int rowCtr)
        {
            NewTopLeagues.Insert(rowCtr, new OrderedClientTopLeague() { SortOrder = (rowCtr + 1) });
            await SortOrderRow();
        }

        public async Task OnDeleteRow(OrderedClientTopLeague league)
        {
            NewTopLeagues.Remove(league);

            if (NewTopLeagues.Count == 0)
                NewTopLeagues.Add(new OrderedClientTopLeague() { SortOrder = 1 });

            await SortOrderRow();
        }

        private async Task SortOrderRow()
        {
            if (NewTopLeagues != null && NewTopLeagues.Any())
            {
                int rowCtr = 0;
                foreach (var league in NewTopLeagues)
                {
                    rowCtr = rowCtr + 1;
                    league.SortOrder = rowCtr;
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
            try
            {
                if (NewTopLeagues.Count == 1 && NewTopLeagues.Any(x => x.LeagueId == 0))
                    NewTopLeagues = new List<OrderedClientTopLeague>();
                else
                    NewTopLeagues.ForEach(x => x.SportName = SportName);

                PostTopLeaguesRequest = new PostTopLeaguesRequest()
                {
                    ClientIdRef = ClientId,
                    SportId = SportId,
                    TopLeagues = NewTopLeagues
                };

                await BackOfficeService.PostTopLeaguesAsync(PostTopLeaguesRequest);
                
                await Modal.OnSaveClickCallback.InvokeAsync();
                await Task.Delay(1000);
                ModalService.Cancel();

            }
            catch (Exception)
            {
            }
        }

        public void Dispose()
        {

        }
        #endregion
    }
}
