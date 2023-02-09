using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.SBC.TopLeagues;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class TopLeagues : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }

        [CascadingParameter(Name = "UserModel")] protected UserModel UserModel { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        public GetTopLeaguesRequest GetTopLeaguesRequest { get; set; }
        public TopLeaguesModel TopLeaguesList { get; set; }
        public TopLeaguesFilterModel TopLeaguesFilter { get; set; }
        public MetaData MetaData { get; set; } = new MetaData();

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Top Leagues";
            IconClass = "ico ico-top-leagues";

            GetTopLeaguesRequest = new GetTopLeaguesRequest();
            TopLeaguesFilter = new TopLeaguesFilterModel();

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 25,
                TotalCount = 0,
                TotalPages = 1,
            };
        }

        public async Task OnInitEvent(TopLeaguesFilterModel model)
        {
            TopLeaguesFilter = model;

            GetTopLeaguesRequest = new GetTopLeaguesRequest()
            {
                ClientIdRefs = model.ClientIds,
                SportId = int.Parse(model.SportId),
                LanguageCode = "en"
            };

            TopLeaguesList = (await BackOfficeService.GetTopLeaguesAsync(GetTopLeaguesRequest)).Result;

            if (TopLeaguesList != null)
            {
                TopLeaguesList.ClientTopLeagues = TopLeaguesList.ClientTopLeagues.Join(model.Clients,
                    le => le.ClientIdRef,
                    c => c.ClientId,
                    (le, c) => new TopLeaguesModel.ClientTopLeague
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
                TopLeaguesList.ClientTopLeagues = new List<TopLeaguesModel.ClientTopLeague>();
            }

            MetaData.TotalCount = TopLeaguesList.ClientTopLeagues.Count;
            MetaData.TotalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(MetaData.TotalCount) / Convert.ToDecimal(MetaData.PageSize)));

            var skip = (MetaData.CurrentPage - 1) * MetaData.PageSize;

            if (MetaData.CurrentPage > 0)
            {
                TopLeaguesList.ClientTopLeagues = TopLeaguesList.ClientTopLeagues
                    .Skip(skip).Take(MetaData.PageSize)
                    .Select((s, index) => new TopLeaguesModel.ClientTopLeague
                    {
                        RowNo = index + 1,
                        ClientIdRef = s.ClientIdRef,
                        ClientName = s.ClientName,
                        TopLeagues = s.TopLeagues
                    })
                    .ToList();
            }
        }

        public async Task OnFilterEvent(TopLeaguesFilterModel model)
        {
            await OnInitEvent(model);
        }

        public async Task OnEditClickEvent(TopLeaguesModel.ClientTopLeague model)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("ClientId", model.ClientIdRef);
            parameters.Add("CurrentTopLeagues", model.TopLeagues);
            parameters.Add("SportId", TopLeaguesFilter.SportId);
            parameters.Add("SportName", TopLeaguesFilter.SportName);

            var options = new ModalOptions();
            ModalService.Open<EditModal>($"Set the Top Leagues of {model.ClientName} ({TopLeaguesFilter.SportName})", parameters, options);
        }

        public async Task OnCopyClickEvent(TopLeaguesModel.ClientTopLeague model)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("CopyFromClientId", model.ClientIdRef);
            parameters.Add("CopyFromClientTopLeagues", model.TopLeagues);
            parameters.Add("SportId", TopLeaguesFilter.SportId);
            parameters.Add("SportName", TopLeaguesFilter.SportName);

            var options = new ModalOptions();
            ModalService.Open<CopyModal>($"Copy {model.ClientName} Top League Settings ({TopLeaguesFilter.SportName}) To", parameters, options);
        }

        protected async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;

            await OnInitEvent(TopLeaguesFilter);
        }

        public async Task OnSaveClickEvent()
        {
            var filter = new TopLeaguesFilterModel()
            {
                Search = TopLeaguesFilter.Search,
                SportId = TopLeaguesFilter.SportId,
                SportName = TopLeaguesFilter.SportName,
                ClientIds = TopLeaguesFilter.ClientIds,
                Clients = TopLeaguesFilter.Clients,
            };
                 
            await OnInitEvent(filter);
        }

        public void Dispose()
        {
            TopLeaguesList = null;
            TopLeaguesFilter = null;

            BackOfficeService.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
