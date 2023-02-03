using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Backoffice.Web.Components.Common;

namespace Backoffice.Web.Components.Maintenance
{
    public partial class ConfirmationModal
    {
        [Inject] IModalService ModalService { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        protected override async Task OnInitializedAsync()
        {
        }
        public async Task OnSaveClickedEvent()
        {
            await Modal.OnSaveClickCallback.InvokeAsync();
        }
    }
}
