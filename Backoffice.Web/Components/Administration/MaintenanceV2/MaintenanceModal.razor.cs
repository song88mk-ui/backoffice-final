using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Backoffice.Web.Components.Administration.MaintenanceV2
{
    public partial class MaintenanceModal
    {
        [Inject] private IModalService ModalService { get; set; }
        protected override void OnInitialized()
        {
            ((ModalService)ModalService).OnOpenV2 += Open;
            ((ModalService)ModalService).CloseModal += Close;
        }
        public void Open()
        {

        }
        public void Close()
        {

            StateHasChanged();
        }
    }
}
