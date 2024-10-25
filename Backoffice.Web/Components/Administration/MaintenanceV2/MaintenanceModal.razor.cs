using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Backoffice.Web.Components.Administration.MaintenanceV2
{
    public partial class MaintenanceModal
    {
        [Inject] private IModalService ModalService { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public MinimalistModal Modal { get; set; }

        private bool _value;
        private bool _isMaintenanceSwitch;

        protected override async Task OnInitializedAsync()
        {
            _isMaintenanceSwitch = ModalParameters.TryGet<bool>("IsMainteanceSwitch");
            _value = ModalParameters.TryGet<bool>("Value");
        }
        public async Task HandleApplyClick()
        {
            ModalService.Cancel();

            await Modal.OnSaveClickCallback.InvokeAsync(new Tuple<bool, bool>(_isMaintenanceSwitch, _value));
        }
        public async Task HandleCancelClick()
        {
            ModalService.Cancel();
        }
    }
}
