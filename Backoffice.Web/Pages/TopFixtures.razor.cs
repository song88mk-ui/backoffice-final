using Backoffice.Shared.Models;
using Backoffice.Web.Components.TopFixtures;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Pages
{
    public partial class TopFixtures : IDisposable
    {
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public MetaData MetaData { get; set; } = new MetaData();
        public TopFixturesModel TopFixturesModel { get; set; }

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Top Fixtures";
            IconClass = "ico-top-fixtures";

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 100,
                TotalCount = 0,
                TotalPages = 1,
            };

            TopFixturesModel = new TopFixturesModel();
        }

        public async Task OnInitEvent(TopFixtureCallback request)
        {
            var clientIds = string.Join(",", request.SelectedClients.Select(s => s.ClientId).ToList());
            var result = (await BackOfficeService.GetClientTopFixtures(new GetClientTopFixtureRequest { ClientIds = clientIds })).Result;

            if (result != null && result.TopFixtures.Any())
            {
                TopFixturesModel.TopFixtures = result.TopFixtures;
            }
            else
            {
                TopFixturesModel.TopFixtures = new List<ClientTopFixtureResponse.OrderedClientTopFixture>();
                foreach(var client in request.SelectedClients)
                {
                    TopFixturesModel.TopFixtures.Add(new ClientTopFixtureResponse.OrderedClientTopFixture 
                    { 
                        ClientIdRef = (int)client.ClientId,  
                        ClientName = client.ClientName,
                        TopFixtures = new List<FixtureModel>()
                    });
                }
            }

            await OnSearchEvent(request.SearchText);
        }

        public async Task OnSearchEvent(string searchString)
        {
            TopFixturesModel.TopFixturesFiltered.Clear();
            if (!string.IsNullOrEmpty(searchString))
                TopFixturesModel.TopFixturesFiltered.AddRange(TopFixturesModel.TopFixtures.Where(t => t.ClientName.Contains(searchString) || t.TopFixtures.Any(tf => tf.SportName.Contains(searchString) )));
            else
                TopFixturesModel.TopFixturesFiltered.AddRange(TopFixturesModel.TopFixtures);

            MetaData.TotalCount = TopFixturesModel.TopFixturesFiltered.Count();
        }

        public async Task OnEdit(ClientTopFixtureResponse.OrderedClientTopFixture topFixture)
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("TopFixture", topFixture);

            var options = new ModalOptions();
            ModalService.Open<EditModal>($"Set Top Fixtures of {topFixture.ClientName}", parameters, options);
        }

        public async Task OnCopy(ClientTopFixtureResponse.OrderedClientTopFixture topFixture)
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");

            var options = new ModalOptions();
            ModalService.Open<CopyModal>($"Copy {topFixture.ClientName} Top Fixtures Settings To", parameters, options);
        }

        protected async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;
            //await GetContents();
        }

        public void Dispose()
        {
        }
        #endregion
    }
}
