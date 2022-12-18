using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.TopLeagues;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Response.GetTopLeaguesResponse;

namespace Backoffice.Web.Pages
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
        public GetTopLeaguesResponse TopLeaguesList { get; set; }
        public ClientResponse ClientInformation { get; set; }
        public List<long> UserClientCodes { get; set; }
        public TopLeaguesFilterModel TopLeaguesFilter { get; set; }
        public MetaData MetaData { get; set; } = new MetaData();
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            try
            {
                PageTitle = "Top Leagues";
                IconClass = "ico-top-leagues";

                ClientInformation = (await BackOfficeService.GetClients()).Result;
                UserClientCodes = ClientInformation != null ? ClientInformation?.Clients?.Where(x => UserModel.ClientCodes.Contains(x.ClientCode)).Select(x => x.ClientId).ToList() : new List<long>();

                GetTopLeaguesRequest = new GetTopLeaguesRequest();
                TopLeaguesFilter = new TopLeaguesFilterModel();
                MetaData = new MetaData()
                {
                    PageSize = 10,
                    CurrentPage = 1
                };

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task OnInitEvent(TopLeaguesFilterModel topLeaguesFilterModel)
        {
            try
            {
                TopLeaguesFilter = topLeaguesFilterModel ?? new TopLeaguesFilterModel()
                {
                    ClientId = string.Join(",", UserClientCodes),
                    SportId = "1",
                    SportName = "Soccer",
                    Search = string.Empty
                };

                GetTopLeaguesRequest = new GetTopLeaguesRequest()
                {
                    ClientIdRefs = string.IsNullOrWhiteSpace(topLeaguesFilterModel?.ClientId) ? string.Join(",", UserClientCodes) : topLeaguesFilterModel?.ClientId,
                    SportId = string.IsNullOrWhiteSpace(topLeaguesFilterModel?.SportId) ? 1 : int.Parse(topLeaguesFilterModel?.SportId),
                    LanguageCode = "en"
                };

                TopLeaguesList = (await BackOfficeService.GetTopLeaguesAsync(GetTopLeaguesRequest)).Result;
                if (TopLeaguesList != null)
                {
                    TopLeaguesList.ClientTopLeagues.ToList().ForEach(c => c.ClientName = ClientInformation?.Clients?.FirstOrDefault(x => x.ClientId == c.ClientIdRef).ClientName);

                    if (topLeaguesFilterModel != null && !string.IsNullOrWhiteSpace(topLeaguesFilterModel.Search))
                    {
                        var result = TopLeaguesList.ClientTopLeagues.Where(t => t.ClientName.ToUpper().Contains(topLeaguesFilterModel.Search.ToUpper()) || t.TopLeagues.Any(tf => tf.SportName.ToUpper().Contains(topLeaguesFilterModel.Search.ToUpper())) || t.TopLeagues.Any(tf => tf.SegmentName.ToUpper().Contains(topLeaguesFilterModel.Search.ToUpper())) || t.TopLeagues.Any(tf => tf.LeagueName.ToUpper().Contains(topLeaguesFilterModel.Search.ToUpper()))).ToList();
                        if (result != null)
                        {
                            TopLeaguesList.ClientTopLeagues.Clear();
                            TopLeaguesList.ClientTopLeagues.AddRange(result);
                        }
                    }
                }
                else
                {
                    TopLeaguesList = new GetTopLeaguesResponse();
                    TopLeaguesList.ClientTopLeagues = new List<OrderedClientTopLeagues>();
                }

                MetaData.TotalCount = TopLeaguesList.ClientTopLeagues.Count;
                MetaData.TotalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(MetaData.TotalCount) / Convert.ToDecimal(MetaData.PageSize)));
                var skip = (MetaData.CurrentPage - 1) * MetaData.PageSize;
                if (MetaData.CurrentPage > 0)
                    TopLeaguesList.ClientTopLeagues = TopLeaguesList.ClientTopLeagues.Skip(skip).Take(MetaData.PageSize).ToList();
            }
            catch (Exception)
            {
            }
        }

        public async Task OnFilterEvent(TopLeaguesFilterModel topLeaguesFilterModel)
        {
            await OnInitEvent(topLeaguesFilterModel);
        }

        public async Task OnEdit(OrderedClientTopLeagues orderedClientTopLeagues)
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("ClientId", orderedClientTopLeagues.ClientIdRef);
            parameters.Add("CurrentTopLeagues", orderedClientTopLeagues.TopLeagues);
            parameters.Add("SportId", TopLeaguesFilter.SportId);
            parameters.Add("SportName", TopLeaguesFilter.SportName);

            var options = new ModalOptions();
            ModalService.Open<EditModal>($"Set the Top Leagues of {orderedClientTopLeagues.ClientName} ({TopLeaguesFilter.SportName})", parameters, options);
        }

        public async Task OnCopy(OrderedClientTopLeagues orderedClientTopLeagues)
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("CopyFromClientId", orderedClientTopLeagues.ClientIdRef);
            parameters.Add("CopyFromClientTopLeagues", orderedClientTopLeagues.TopLeagues);
            parameters.Add("SportId", TopLeaguesFilter.SportId);
            parameters.Add("SportName", TopLeaguesFilter.SportName);

            var options = new ModalOptions();
            ModalService.Open<CopyModal>($"Copy {orderedClientTopLeagues.ClientName} Top League Settings ({TopLeaguesFilter.SportName}) To", parameters, options);
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
                ClientId = TopLeaguesFilter.ClientId
            };
                 
            await OnInitEvent(filter);
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
