using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Administration.ManageUserGroups
{
    public partial class ViewUsersModal : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        protected List<UserResponse> Users { get; set; }
        protected string UserGroupName { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Users = ModalParameters.TryGet<List<UserResponse>>("Users");
            UserGroupName = ModalParameters.TryGet<string>("UserGroupName");
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
