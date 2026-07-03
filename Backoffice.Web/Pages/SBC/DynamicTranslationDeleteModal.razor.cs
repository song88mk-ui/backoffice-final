using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class DynamicTranslationDeleteModal
    {
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        private string StringId { get; set; } = string.Empty;
        private Func<Task<bool>> OnDeleteCallback { get; set; }

        protected override void OnInitialized()
        {
            StringId = ModalParameters.TryGet<string>("StringId") ?? string.Empty;
            OnDeleteCallback = ModalParameters.TryGet<Func<Task<bool>>>("OnDeleteCallback");
        }

        private async Task ConfirmDelete()
        {
            if (OnDeleteCallback != null)
            {
                var success = await OnDeleteCallback.Invoke();
                if (success)
                {
                    _modalService.Close();
                }
            }
        }

        private void CancelDelete()
        {
            _modalService.Cancel();
        }
    }
}
