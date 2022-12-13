using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Requests.GetTopLeaguesRequest;

namespace Backoffice.Web.Components.TopLeagues
{
    public partial class CopyModal : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [Parameter] public CopyTopLeaguesModel CopyTopLeaguesModel { get; set; }
        public CopyTopLeaguesRequest CopyTopLeaguesRequest { get; set; }
        public bool IsSelectAll { get; set; }
        public string ClientIdErrorMessage { get; set; }
        public string AlertMessage { get; set; }
        public string AlertStatus { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ClientIdErrorMessage = string.Empty;

            CopyTopLeaguesModel = new CopyTopLeaguesModel
            {
                CopyFromClientId = ModalParameters.TryGet<int>("CopyFromClientId"),
                SportId = int.Parse(ModalParameters.TryGet<string>("SportId")),
                CopyFromClientTopLeagues = ModalParameters.TryGet<List<OrderedClientTopLeague>>("CopyFromClientTopLeagues") ?? new List<OrderedClientTopLeague>(),

                ClientIdSelectedModel = new List<ClientIdSelectedModel>()
            };

            var clients = (await BackOfficeService.GetClients()).Result;
            if (clients != null)
            {
                if (UserModel.ClientCodes.Any())
                    clients.Clients = clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper()) && c.ClientId != CopyTopLeaguesModel.CopyFromClientId).ToList();

                CopyTopLeaguesModel.ClientIdSelectedModel = clients.Clients.Select(s => new ClientIdSelectedModel
                {
                    IsSelected = false,
                    ClientId = s.ClientId.ToString(),
                    ClientName = s.ClientName
                }).ToList();
            }

            if (!CopyTopLeaguesModel.ClientIdSelectedModel.Any())
                CopyTopLeaguesModel.ClientIdSelectedModel.Add(new ClientIdSelectedModel
                {
                    IsSelected = false,
                    ClientId = string.Empty,
                    ClientName = "No Clients Available"
                });

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

                var topSetUp = new List<CopyTopLeaguesRequest.SportLeague>();
                var i = 0;
                foreach (var item in CopyTopLeaguesModel.CopyFromClientTopLeagues)
                {
                    topSetUp.Add(new CopyTopLeaguesRequest.SportLeague()
                    {
                        SportId = item.SportId,
                        SportName = item.SportName,
                        SegmentId = item.SegmentId,
                        SegmentName = item.SegmentName,
                        LeagueId = item.LeagueId,
                        LeagueName = item.LeagueName,
                        Status = item.Status,
                        LiveCount = item.LiveCount,
                        SortOrder = i++
                    });
                }

                CopyTopLeaguesRequest = new CopyTopLeaguesRequest()
                {
                    ClientIdRefs = CopyTopLeaguesModel.ClientIdSelectedModel.Where(x => x.IsSelected).Select(x => int.Parse(x.ClientId)).ToList(),
                    SportId = CopyTopLeaguesModel.SportId,
                    TopLeagues = topSetUp
                };

                var result = await BackOfficeService.CopyTopLeaguesAsync(CopyTopLeaguesRequest);
                if (result.Result)
                {
                    AlertMessage = "Copy top leagues successful";
                    AlertStatus = "alert-success";
                    await Modal.OnSaveClickCallback.InvokeAsync();
                    await Task.Delay(3500);
                    ModalService.Cancel();
                }
                else
                {
                    AlertStatus = "alert-danger";
                    AlertMessage = "Copy top leagues failed";
                }
            }
        }

        public void ToggleCheckbox(string clientId)
        {
            foreach (var item in CopyTopLeaguesModel.ClientIdSelectedModel)
            {
                if (item.ClientId == clientId)
                    item.IsSelected = !item.IsSelected;
            }

            var count = CopyTopLeaguesModel.ClientIdSelectedModel.Where(_ => _.IsSelected).Count();
            ClientIdErrorMessage = count == 0 ? "Select client to proceed" : string.Empty;
        }

        public void RemoveClient(string clientId)
        {
            var item = CopyTopLeaguesModel.ClientIdSelectedModel.FirstOrDefault(x => x.ClientId == clientId);
            if (item != null)
                CopyTopLeaguesModel.ClientIdSelectedModel.Remove(item);
        }

        public void RemoveLeague(int leagueId)
        {
            if (CopyTopLeaguesModel.CopyFromClientTopLeagues.Count != 1)
            {
                var item = CopyTopLeaguesModel.CopyFromClientTopLeagues.FirstOrDefault(x => x.LeagueId == leagueId);
                if (item != null)
                    CopyTopLeaguesModel.CopyFromClientTopLeagues.Remove(item);
            }
            else
            {
                //require
            }
        }

        public void SelectAllToggleCheckbox(bool isSelectAll)
        {
            CopyTopLeaguesModel.ClientIdSelectedModel.ForEach(x => x.IsSelected = isSelectAll);
            ClientIdErrorMessage = string.IsNullOrWhiteSpace(ClientIdErrorMessage) ? "Select client to proceed" : string.Empty;
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