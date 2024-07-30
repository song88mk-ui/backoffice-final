using Backoffice.Shared.Enums;
using Backoffice.Web.States;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;

namespace Backoffice.Web.Shared
{
    public partial class LanguageDropDown
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Parameter] public EventCallback OnLanguageCodeChangedCallback { get; set; }
        [Parameter] public string DropDownButtonClassName { get; set; }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var cookieValue = await JSRuntime.InvokeAsync<string>("getCookie", "c-langCode");

                await OnLanguageCodeChanged(cookieValue);
            }
        }

        private async Task OnLanguageCodeChanged(string value)
        {
            if (TranslationState.ActiveLanguageCode.ToString() == value)
                return;

            if (Enum.TryParse(value, out LanguageCode langCode))
            {
                await JSRuntime.InvokeVoidAsync("setCookie","c-langCode", value, 30);
                TranslationState.ActiveLanguageCode = langCode;
                await OnLanguageCodeChangedCallback.InvokeAsync();

                await InvokeAsync(StateHasChanged);
            }
        }
    }
}
