using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class StaticTranslationEditModal
    {
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        private string TranslationKey { get; set; } = string.Empty;
        private string TranslationValue { get; set; } = string.Empty;
        private Func<string, Task> OnSaveCallback { get; set; }

        protected override void OnInitialized()
        {
            TranslationKey = ModalParameters.TryGet<string>("TranslationKey") ?? string.Empty;
            TranslationValue = ModalParameters.TryGet<string>("TranslationValue") ?? string.Empty;
            OnSaveCallback = ModalParameters.TryGet<Func<string, Task>>("OnSaveCallback");
        }

        private async Task SaveEdit()
        {
            if (OnSaveCallback != null)
            {
                await OnSaveCallback.Invoke(TranslationValue);
            }

            // Modal.Close(ModalResult.Ok(TranslationValue));
        }

        private void CancelEdit()
        {
            // Modal.Cancel();
        }
    }
}
