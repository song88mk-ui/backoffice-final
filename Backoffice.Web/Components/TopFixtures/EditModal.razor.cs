using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.TopFixtures
{
    public partial class EditModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        public ClientTopFixtureResponse.OrderedClientTopFixture TopFixture { get; set; }

        public ClientTopFixtureResponse.OrderedClientTopFixture NewTopFixture { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            try
            {
                TopFixture = ModalParameters.TryGet<ClientTopFixtureResponse.OrderedClientTopFixture>("TopFixture");
            }
            catch (Exception)
            {

            }
        }

        public async Task OnInitEvent()
        {
            NewTopFixture = new ClientTopFixtureResponse.OrderedClientTopFixture();
            if (TopFixture.TopFixtures.Any())
            {
                NewTopFixture.TopFixtures = new List<Backoffice.Shared.Models.FixtureModel>();
                NewTopFixture.TopFixtures.AddRange(TopFixture.TopFixtures);
            }

        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnSaveClickEvent()
        {

        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
