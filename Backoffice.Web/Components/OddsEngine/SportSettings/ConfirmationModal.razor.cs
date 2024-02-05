using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class ConfirmationModal : IDisposable
    {
        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] IModalService ModalService { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }

        private int _userAction; // 0 - cancel, 1 - save update
        private bool IsUpdating;
        private bool IsPreLoader;
        private string _buttonYesDisplay;
        protected override async Task OnInitializedAsync()
        {
            _userAction = ModalParameters?.TryGet<string>("UserAction").ToUpper() switch
            {
                "CANCELLING" => 0,
                "LEAVING" => 1,
                _ => -1
            };

            _buttonYesDisplay = _userAction switch
            {
                0 => "Yes, Cancel",
                1 => "Save Changes",
                _ => "-"
            };
        }

        private async void OnYesBtnClicked()
        {
            await Modal.OnSaveClickCallback.InvokeAsync(_userAction);
            ModalService.Cancel();
        }
        private async void OnCacnelClicked()
        {
            await Modal.OnCancelClickCallback.InvokeAsync(_userAction);
            ModalService.Cancel();
        }
        public void Dispose()
        {
        }
    }
}
