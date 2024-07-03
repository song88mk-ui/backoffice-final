using Backoffice.Shared.Enums;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Shared
{
    public partial class LanguageDropDown
    {
        [Parameter] public EventCallback OnLanguageCodeChangedCallback { get; set; }

        private async void OnLanguageCodeChanged(string value)
        {
            if (TranslationState.ActiveLanguageCode.ToString() == value)
                return;

            if (Enum.TryParse(value, out LanguageCode langCode))
            {
                TranslationState.ActiveLanguageCode = langCode;
                await OnLanguageCodeChangedCallback.InvokeAsync();
            }
        }
    }
}
