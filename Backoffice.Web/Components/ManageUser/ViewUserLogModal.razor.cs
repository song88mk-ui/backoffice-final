using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.ManageUser
{
    public partial class ViewUserLogModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        private UserResponse User { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            User = ModalParameters.TryGet<UserResponse>("UserResponse");
        }
        public async Task OnCloseModalClickEvent()
        {
            ModalService.Cancel();
        }
        public void Dispose()
        {
            
        }
        #endregion
    }
}
