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
        private string SourceText { get; set; } = string.Empty;
        private string TargetText { get; set; } = string.Empty;
        private string SelectedLanguageCode { get; set; } = string.Empty;
        private bool IsNew { get; set; }
        private Func<string, string, string, Task<bool>> OnSaveCallback { get; set; }

        protected override void OnInitialized()
        {
            TranslationKey = ModalParameters.TryGet<string>("TranslationKey") ?? string.Empty;
            SourceText = ModalParameters.TryGet<string>("SourceText") ?? string.Empty;
            TargetText = ModalParameters.TryGet<string>("TargetText") ?? string.Empty;
            SelectedLanguageCode = ModalParameters.TryGet<string>("SelectedLanguageCode") ?? string.Empty;
            IsNew = ModalParameters.TryGet<bool>("IsNew");
            OnSaveCallback = ModalParameters.TryGet<Func<string, string, string, Task<bool>>>("OnSaveCallback");
        }

        private async Task SaveEdit()
        {
            if (OnSaveCallback != null && !string.IsNullOrWhiteSpace(TranslationKey) && !string.IsNullOrWhiteSpace(SourceText))
            {
                var success = await OnSaveCallback.Invoke(TranslationKey, SourceText, TargetText);
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
