using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class StaticTranslationDeleteModal
    {
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        private string TranslationKey { get; set; } = string.Empty;
        private Func<Task> OnDeleteCallback { get; set; }

        protected override void OnInitialized()
        {
            TranslationKey = ModalParameters.TryGet<string>("TranslationKey") ?? string.Empty;
            OnDeleteCallback = ModalParameters.TryGet<Func<Task>>("OnDeleteCallback");
        }

        private async Task ConfirmDelete()
        {
            if (OnDeleteCallback != null)
            {
                await OnDeleteCallback.Invoke();
            }

            // Modal.Close(ModalResult.Ok(TranslationKey));
        }

        private void CancelDelete()
        {
            // Modal.Cancel();
        }
    }
}
