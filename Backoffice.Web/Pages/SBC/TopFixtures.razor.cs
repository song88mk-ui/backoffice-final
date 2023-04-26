using Backoffice.Shared.Models;
using Backoffice.Web.Components.SBC.TopFixtures;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Backoffice.Shared.Models.Requests;
using Microsoft.JSInterop;

namespace Backoffice.Web.Pages.SBC
{
    public partial class TopFixtures : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] private ToastService? ToasterService { get; set; }


        public MetaData MetaData { get; set; }
        public TopFixturesModel TopFixturesModel { get; set; }
        public bool IsPreLoader { get; set; } = true;

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        #endregion
        private ToastService _toasterService => ToasterService!;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Top Fixtures";
            IconClass = "ico ico-top-fixtures";
            IsPreLoader = true;

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 100,
                TotalCount = 0,
                TotalPages = 1,
            };

            TopFixturesModel = new TopFixturesModel();
        }

        public async Task OnInitEvent(TopFixtureCallbackModel request)
        {
            if (!IsPreLoader)
            {
                IsPreLoader = true;
                StateHasChanged();
            }

            var clientIds = string.Join(",", request.Clients.Select(s => s.ClientId).ToList());
            var topFixtureResponse = (await BackOfficeService.GetClientTopFixtures(new GetClientTopFixtureRequest { ClientIds = clientIds }));

            if (topFixtureResponse.StatusCode != Backoffice.Shared.Enums.StatusCode.Success)
            {
                _toasterService.AddToast(Toast.NewToast
                        ("Client Top Fixtures",
                        $"{topFixtureResponse.Message}"));
                IsPreLoader = false;
                return;
            }

            var result = topFixtureResponse.Result;

            TopFixturesModel = new TopFixturesModel();
            if (result != null && result.TopFixtures.Any())
            {
                TopFixturesModel.TopFixtures = result.TopFixtures;
            }
            else
            {
                TopFixturesModel.TopFixtures = new List<ClientTopFixtureModel.OrderedClientTopFixture>();

                foreach (var client in request.Clients)
                {
                    TopFixturesModel.TopFixtures.Add(new ClientTopFixtureModel.OrderedClientTopFixture
                    {
                        ClientIdRef = (int)client.ClientId,
                        ClientName = client.ClientName,
                        TopFixtures = new List<ClientTopFixtureModel.TopFixture>()
                    });
                }
            }

            await OnSearchEvent(request.SearchText);

            IsPreLoader = false;
        }

        public async Task OnSearchEvent(string searchString)
        {
            TopFixturesModel.TopFixturesFiltered.Clear();

            if (!string.IsNullOrEmpty(searchString))
            {
                TopFixturesModel.TopFixturesFiltered.AddRange(TopFixturesModel.TopFixtures.Where(t => t.ClientName.Contains(searchString) || t.TopFixtures.Any(tf => tf.SportName.Contains(searchString))));
            }
            else
            {
                TopFixturesModel.TopFixturesFiltered.AddRange(TopFixturesModel.TopFixtures);
            }

            MetaData.TotalCount = TopFixturesModel.TopFixturesFiltered.Count();
        }

        public async Task OnEditClickEvent(ClientTopFixtureModel.OrderedClientTopFixture topFixture)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("TopFixture", topFixture);

            var options = new ModalOptions();

            ModalService.Open<EditModal>($"Set Top Fixtures of {topFixture.ClientName}", parameters, options);
        }

        public async Task OnCopyClickEvent(ClientTopFixtureModel.OrderedClientTopFixture topFixture)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("TopFixture", topFixture);

            var options = new ModalOptions();

            ModalService.Open<CopyModal>($"Copy {topFixture.ClientName} Top Fixtures Settings To", parameters, options);
        }

        public async Task OnSaveClickEvent(object item)
        {
            var clientIdRefs = string.IsNullOrEmpty(item.ToString()) ? new List<string>() : item.ToString().Split(",").ToList();
            foreach (var clientIdRef in clientIdRefs)
            {
                if (string.IsNullOrEmpty(clientIdRef))
                    continue;

                var clientTopFixture = TopFixturesModel.TopFixtures.FirstOrDefault(t => t.ClientIdRef == Convert.ToInt32(clientIdRef));
                if (clientTopFixture != null)
                {
                    var result = (await BackOfficeService.GetClientTopFixtures(new GetClientTopFixtureRequest { ClientIds = item.ToString() })).Result;

                    clientTopFixture.TopFixtures = result.TopFixtures.FirstOrDefault()?.TopFixtures ?? new List<ClientTopFixtureModel.TopFixture>();
                }
            }
        }

        protected async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;
        }

        public void Dispose()
        {
            TopFixturesModel = null;

            BackOfficeService.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
