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
        private bool IsNew { get; set; }
        private Func<string, string, Task<bool>> OnSaveCallback { get; set; }

        protected override void OnInitialized()
        {
            TranslationKey = ModalParameters.TryGet<string>("TranslationKey") ?? string.Empty;
            TranslationValue = ModalParameters.TryGet<string>("TranslationValue") ?? string.Empty;
            IsNew = ModalParameters.TryGet<bool>("IsNew");
            OnSaveCallback = ModalParameters.TryGet<Func<string, string, Task<bool>>>("OnSaveCallback");
        }

        private async Task SaveEdit()
        {
            if (OnSaveCallback != null && !string.IsNullOrWhiteSpace(TranslationKey))
            {
                var success = await OnSaveCallback.Invoke(TranslationKey, TranslationValue);
                if (success)
                {
                    _modalService.Close();
                }
            }
        }

        private void CancelEdit()
        {
            _modalService.Cancel();
        }
    }
}
