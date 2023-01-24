using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Administration.ManageUserGroups
{
    public partial class DeleteUserGroupModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        #endregion
        #region Methods
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
