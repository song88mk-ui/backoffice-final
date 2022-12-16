using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.SubUser
{
    public partial class CreateOrUpdateModal : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        public SubUserClaimsModel SubUserClaims { get; set; }
        #endregion
        protected override async Task OnInitializedAsync()
        {
            try
            {
                SubUserClaims = ModalParameters.TryGet<SubUserClaimsModel>("SubUserClaims");

            }
            catch (Exception)
            {

            }
        }
        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }
        public async Task OnSaveClickEvent()
        {
            //var topFixtures = new List<ClientTopFixtureSetup>();

            //if (NewTopFixture.TopFixtures != null && NewTopFixture.TopFixtures.Any())
            //{
            //    int rowCtr = 0;
            //    foreach (var fixture in NewTopFixture.TopFixtures)
            //    {
            //        if (string.IsNullOrEmpty(fixture.FixtureId))
            //            continue;

            //        rowCtr = rowCtr + 1;
            //        topFixtures.Add(new ClientTopFixtureSetup
            //        {
            //            FixtureId = Convert.ToInt64(fixture.FixtureId),
            //            SortOrder = rowCtr
            //        });
            //    }
            //}

            //if (!topFixtures.Any())
            //    return;

            //var request = new PostClientTopFixtureRequest()
            //{
            //    ClientId = TopFixture.ClientIdRef,
            //    TopFixtures = topFixtures
            //};
            //await BackOfficeService.PostClientTopFixtures(request);

            ModalService.Cancel();

            //await Modal.OnSaveClickCallback.InvokeAsync(TopFixture.ClientIdRef.ToString());
        }
        public void Dispose()
        {

        }
    }
}
