using Backoffice.Shared.Models;
using Backoffice.Web.Components.TopFixtures;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Backoffice.Shared.Models.Requests;

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
        public bool IsPreLoader { get; set; }
        public TopFixturesModel TopFixturesModel { get; set; }

        public string ClientName { get; set; }

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Top Fixtures";
            IconClass = "ico-top-fixtures";

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 10000,
                TotalPages = 100,
            };

            TopFixturesModel = new TopFixturesModel();

        }

        public async Task OnInitEvent(string clientId)
        {
            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);

            TopFixturesModel.TopFixtures = (await BackOfficeService.GetClientTopFixtures(new GetClientTopFixtureRequest
            {
                ClientIds = clientId
            })).Result.TopFixtures;


            IsPreLoader = false;
        }

        public async Task OnSearchEvent(string searchString)
        {
            //if(!string.IsNullOrEmpty(searchString))
            //    TopFixturesModel.TopFixtures = TopFixturesModel.TopFixtures.Where()
        }

        public async Task OnEdit()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");

            var options = new ModalOptions();
            ModalService.Open<EditModal>($"Set Top Leagues of {ClientName}", parameters, options);
        }

        public async Task OnCopy()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");

            var options = new ModalOptions();
            ModalService.Open<CopyModal>($"Copy {ClientName} Top Fixtures Settings To", parameters, options);
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
