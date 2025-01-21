using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Requests.GetTopLeaguesRequest;

namespace Backoffice.Web.Components.SBC.TopLeagues
{
    public partial class CopyModal : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        [Parameter] public CopyTopLeaguesModel CopyTopLeaguesModel { get; set; }

        public CopyTopLeaguesRequest CopyTopLeaguesRequest { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }

        public bool IsSelectAll { get; set; }
        public string ClientIdErrorMessage { get; set; }
        public int SelectedClientCount { get; set; }
        public string SelectedClientName { get; set; }
        public string AlertMessage { get; set; }
        public string AlertStatus { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ClientIdErrorMessage = string.Empty;
            Clients = new List<SelectListItemWithCheckboxModel<string>>();

            var topLeagues = ModalParameters.TryGet<List<TopLeague>>("CopyFromClientTopLeagues") ?? new List<TopLeague>();

            CopyTopLeaguesModel = new CopyTopLeaguesModel
            {
                CopyFromClientId = ModalParameters.TryGet<int>("CopyFromClientId"),
                SportId = int.Parse(ModalParameters.TryGet<string>("SportId")),
                CopyFromClientTopLeagues = topLeagues.Select(s => new CopyTopLeaguesModel.TopLeague
                {
                    SportId = s.SportId,
                    SportName = s.SportName,
                    SegmentId = s.SegmentId,
                    SegmentName = s.SegmentName,
                    LeagueId = s.LeagueId,
                    LeagueName = s.LeagueName,
                    Status = s.Status,
                    LiveCount = s.LiveCount,
                    SortOrder = s.SortOrder
                }).ToList(),
                ClientIdSelectedModel = new List<ClientIdSelectedModel>()
            };


            if (_userState.Model.SportsbookClients != null && _userState.Model.SportsbookClients.Any())
            {
                CopyTopLeaguesModel.ClientIdSelectedModel = _userState.Model.SportsbookClients.Select(s => new ClientIdSelectedModel
                {
                    IsSelected = false,
                    ClientId = s.ClientId.ToString(),
                    ClientName = s.ClientName
                }).ToList();

                Clients = CopyTopLeaguesModel.ClientIdSelectedModel
                    .Select(s => new SelectListItemWithCheckboxModel<string>
                    {
                        Value = s.ClientId,
                        Name = s.ClientName,
                        IsChecked = false
                    }).ToList();

                Clients.Insert(0, new SelectListItemWithCheckboxModel<string>
                {
                    Value = "all",
                    Name = "All Clients",
                    IsChecked = false
                });

                SelectedClientCount = Clients.Where(c => c.Value != "all" && c.IsChecked).Count();
            }

            CopyTopLeaguesRequest = new CopyTopLeaguesRequest();
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnSaveClickEvent()
        {
            if (!CopyTopLeaguesModel.ClientIdSelectedModel.Where(x => x.IsSelected).Any())
            {
                ClientIdErrorMessage = "Select client to proceed";
            }
            else
            {
                ClientIdErrorMessage = string.Empty;

                var topSetUp = CopyTopLeaguesModel.CopyFromClientTopLeagues
                    .Select((s, index) => new CopyTopLeaguesRequest.SportLeague
                    {
                        SportId = s.SportId,
                        SportName = s.SportName,
                        SegmentId = s.SegmentId,
                        SegmentName = s.SegmentName,
                        LeagueId = s.LeagueId,
                        LeagueName = s.LeagueName,
                        Status = s.Status,
                        LiveCount = s.LiveCount,
                        SortOrder = index + 1
                    }).ToList();

                //var topSetUp = new List<CopyTopLeaguesRequest.SportLeague>();

                //var i = 0;
                //foreach (var item in CopyTopLeaguesModel.CopyFromClientTopLeagues)
                //{
                //    topSetUp.Add(new CopyTopLeaguesRequest.SportLeague()
                //    {
                //        SportId = item.SportId,
                //        SportName = item.SportName,
                //        SegmentId = item.SegmentId,
                //        SegmentName = item.SegmentName,
                //        LeagueId = item.LeagueId,
                //        LeagueName = item.LeagueName,
                //        Status = item.Status,
                //        LiveCount = item.LiveCount,
                //        SortOrder = i++
                //    });
                //}

                CopyTopLeaguesRequest = new CopyTopLeaguesRequest()
                {
                    ClientIdRefs = CopyTopLeaguesModel.ClientIdSelectedModel.Where(x => x.IsSelected).Select(x => int.Parse(x.ClientId)).ToList(),
                    SportId = CopyTopLeaguesModel.SportId,
                    TopLeagues = topSetUp
                };

                var result = await BackofficeService.CopyTopLeaguesAsync(CopyTopLeaguesRequest);

                if (result.Result)
                {
                    AlertMessage = "Copy top leagues successful";
                    AlertStatus = "alert-success";
                    await Modal.OnSaveClickCallback.InvokeAsync();
                    await Task.Delay(1000);
                    ModalService.Cancel();
                }
                else
                {
                    AlertStatus = "alert-danger";
                    AlertMessage = "Copy top leagues failed";
                }
            }
        }

        public async Task OnClientCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedClientName = string.Empty;
            SelectedClientCount = items.Count;

            if (SelectedClientCount == 1)
            {
                SelectedClientName = items.FirstOrDefault().Value;
            }

            var selectedIds = items.Select(s => s.Key).ToList();

            foreach (var item in CopyTopLeaguesModel.ClientIdSelectedModel)
            {
                if (selectedIds.Contains(item.ClientId))
                    item.IsSelected = true;
                else
                    item.IsSelected = false;
            }

            ClientIdErrorMessage = SelectedClientCount > 0 ? string.Empty : "Select client to proceed";
        }

        public void RemoveClient(string clientId)
        {
            var item = CopyTopLeaguesModel.ClientIdSelectedModel.FirstOrDefault(x => x.ClientId == clientId);
            if (item != null)
                CopyTopLeaguesModel.ClientIdSelectedModel.Remove(item);
        }

        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}