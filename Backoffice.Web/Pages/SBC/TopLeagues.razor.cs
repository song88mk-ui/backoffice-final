using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.SBC.TopLeagues;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Pages.SBC
{
    public partial class TopLeagues : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] private ToastService? ToasterService { get; set; }

        [CascadingParameter(Name = "UserModel")] protected UserModel UserModel { get; set; }
        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public bool IsPreLoader { get; set; } = true;

        public GetTopLeaguesRequest GetTopLeaguesRequest { get; set; }
        public TopLeaguesModel TopLeaguesList { get; set; }
        public TopLeaguesFilterModel FilterModel { get; set; }
        public MetaData MetaData { get; set; } = new MetaData();

        #endregion
        private ToastService _toasterService => ToasterService!;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Top Leagues";
            IconClass = "ico ico-top-leagues";
            IsPreLoader = true;

            GetTopLeaguesRequest = new GetTopLeaguesRequest();
            FilterModel = new TopLeaguesFilterModel();

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 25,
                TotalCount = 0,
                TotalPages = 1,
            };

            await AuthenticateAccess.InvokeAsync();
        }

        public async Task OnInitEvent(TopLeaguesFilterModel model)
        {
            await GetContent(model);
        }
        public async Task OnFilterEvent(TopLeaguesFilterModel model)
        {
            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 25,
                TotalCount = 0,
                TotalPages = 1,
            };

            await GetContent(model);
        }

        public async Task OnEditClickEvent(ClientTopLeague model)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("ClientId", model.ClientIdRef);
            parameters.Add("CurrentTopLeagues", model.TopLeagues);
            parameters.Add("SportId", FilterModel.SportId);
            parameters.Add("SportName", FilterModel.SportName);

            var options = new ModalOptions();
            ModalService.Open<EditModal>($"Set the Top Leagues of {model.ClientName} ({FilterModel.SportName})", parameters, options);
        }

        public async Task OnCopyClickEvent(ClientTopLeague model)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("CopyFromClientId", model.ClientIdRef);
            parameters.Add("CopyFromClientTopLeagues", model.TopLeagues);
            parameters.Add("SportId", FilterModel.SportId);
            parameters.Add("SportName", FilterModel.SportName);

            var options = new ModalOptions();
            ModalService.Open<CopyModal>($"Copy {model.ClientName} Top League Settings ({FilterModel.SportName}) To", parameters, options);
        }

        protected async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;

            await OnInitEvent(FilterModel);
        }

        public async Task OnSaveClickEvent()
        {
            var filter = new TopLeaguesFilterModel()
            {
                Search = FilterModel.Search,
                SportId = FilterModel.SportId,
                SportName = FilterModel.SportName,
                ClientIds = FilterModel.ClientIds,
                Clients = FilterModel.Clients,
            };

            await OnInitEvent(filter);
        }

        public void Dispose()
        {
            TopLeaguesList = null;
            FilterModel = null;

            BackOfficeService.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities
        private async Task GetContent(TopLeaguesFilterModel model)
        {
            if (!IsPreLoader)
            {
                IsPreLoader = true;
                StateHasChanged();
            }

            FilterModel = model;

            GetTopLeaguesRequest = new GetTopLeaguesRequest()
            {
                ClientIdRefs = model.ClientIds,
                SportId = int.Parse(model.SportId),
                LanguageCode = "en"
            };

            var topLeaguesResponse = await BackOfficeService.GetTopLeaguesAsync(GetTopLeaguesRequest);

            if (topLeaguesResponse.StatusCode != Backoffice.Shared.Enums.StatusCode.Success)
            {
                _toasterService.AddToast(Toast.NewToast
                        ("Client Top Fixtures",
                        $"{topLeaguesResponse.Message}"));
                IsPreLoader = false;
                return;
            }

            TopLeaguesList = topLeaguesResponse.Result;

            if (TopLeaguesList != null)
            {
                TopLeaguesList.ClientTopLeagues = TopLeaguesList.ClientTopLeagues.Join(model.Clients,
                    le => le.ClientIdRef,
                    c => c.ClientId,
                    (le, c) => new ClientTopLeague
                    {
                        ClientIdRef = le.ClientIdRef,
                        ClientName = c.ClientName,
                        TopLeagues = le.TopLeagues
                    }).ToList();

                if (!string.IsNullOrWhiteSpace(model.Search))
                {
                    var result = TopLeaguesList.ClientTopLeagues.Where(c => c.ClientName.ToUpper().Contains(model.Search.ToUpper())
                    || c.TopLeagues.Any(tf => tf.SportName.ToUpper().Contains(model.Search.ToUpper()))
                    || c.TopLeagues.Any(tf => tf.SegmentName.ToUpper().Contains(model.Search.ToUpper()))
                    || c.TopLeagues.Any(tf => tf.LeagueName.ToUpper().Contains(model.Search.ToUpper()))).ToList();

                    if (result != null)
                        TopLeaguesList.ClientTopLeagues = result;
                }
            }
            else
            {
                TopLeaguesList = new TopLeaguesModel();
                TopLeaguesList.ClientTopLeagues = new List<ClientTopLeague>();
            }

            MetaData.TotalCount = TopLeaguesList.ClientTopLeagues.Count;
            MetaData.TotalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(MetaData.TotalCount) / Convert.ToDecimal(MetaData.PageSize)));

            var skip = (MetaData.CurrentPage - 1) * MetaData.PageSize;

            if (MetaData.CurrentPage > 0)
            {
                TopLeaguesList.ClientTopLeagues = TopLeaguesList.ClientTopLeagues
                    .Skip(skip).Take(MetaData.PageSize)
                    .Select((s, index) => new ClientTopLeague
                    {
                        RowNo = index + ((MetaData.CurrentPage - 1) * MetaData.PageSize) + 1,
                        ClientIdRef = s.ClientIdRef,
                        ClientName = s.ClientName,
                        TopLeagues = s.TopLeagues
                    })
                    .ToList();
            }

            IsPreLoader = false;
        }
        #endregion
    }
}