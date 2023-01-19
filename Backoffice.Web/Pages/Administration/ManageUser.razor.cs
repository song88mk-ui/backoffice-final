using Backoffice.Web.Components.ManageUser;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.Administration
{
    public partial class ManageUser : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Manage User";
            IconClass = "ico ico-manage-user";
        }

        public async Task OnCreateUserClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("DialogStyle", "max-width: 65rem;");

            var options = new ModalOptions();

            ModalService.Open<CreateOrUpdateModal>("Create User", parameters, options);
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
