using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class DynamicTranslationEditModal
    {
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        private string StringId { get; set; } = string.Empty;
        private string SourceText { get; set; } = string.Empty;
        private string TargetText { get; set; } = string.Empty;
        private string SelectedLanguageCode { get; set; } = "EN";
        private bool IsNew { get; set; }
        private Func<string, string, string, Task<bool>> OnSaveCallback { get; set; }

        private bool ShowTranslationField => !IsNew && SelectedLanguageCode != "EN";

        protected override void OnInitialized()
        {
            StringId = ModalParameters.TryGet<string>("StringId") ?? string.Empty;
            SourceText = ModalParameters.TryGet<string>("SourceText") ?? string.Empty;
            TargetText = ModalParameters.TryGet<string>("TargetText") ?? string.Empty;
            SelectedLanguageCode = ModalParameters.TryGet<string>("SelectedLanguageCode") ?? "EN";
            IsNew = ModalParameters.TryGet<bool>("IsNew");
            OnSaveCallback = ModalParameters.TryGet<Func<string, string, string, Task<bool>>>("OnSaveCallback");
        }

        private async Task SaveEdit()
        {
            if (OnSaveCallback != null && !string.IsNullOrWhiteSpace(StringId) && !string.IsNullOrWhiteSpace(SourceText))
            {
                var success = await OnSaveCallback.Invoke(StringId, SourceText, TargetText);
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
