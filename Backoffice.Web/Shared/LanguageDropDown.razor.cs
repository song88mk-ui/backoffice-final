using Backoffice.Shared.Enums;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Shared
{
    public partial class LanguageDropDown
    {
        #region Properties

        [Parameter] public string DropDownButtonClassName { get; set; }
        [Parameter] public EventCallback OnLanguageCodeChange { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            var cookieValue = await _jsRuntime.InvokeAsync<string>("getCookie", "c-langCode");
            await UpdateLanguageCode(cookieValue);
        }

        private async Task HandleLanguageCodeClick(string value)
        {
            await UpdateLanguageCode(value);
        }

        #endregion

        #region Utilities

        private async Task UpdateLanguageCode(string value)
        {
            if (_translationState.ActiveLanguageCode.ToString() == value)
                return;

            if (Enum.TryParse(value, out LanguageCode langCode))
            {
                await _jsRuntime.InvokeVoidAsync("setCookie", "c-langCode", value, 30);
                await _translationState.SetLanguageCode(langCode);

                await PageState.Page.RefreshPage();
            }
        }

        #endregion
    }
}
