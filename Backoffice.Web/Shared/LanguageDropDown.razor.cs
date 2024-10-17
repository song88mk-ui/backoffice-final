using Backoffice.Shared.Enums;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics;

namespace Backoffice.Web.Shared
{
    public partial class LanguageDropDown
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public IHttpContextAccessor _httpContextAccessor { get; set; }
        [Parameter] public string DropDownButtonClassName { get; set; }
        [Parameter] public EventCallback OnLanguageCodeChange { get; set; }
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
            if (_translationState.ActiveLanguageCode.ToString() == value)
                return;

            if (Enum.TryParse(value, out LanguageCode langCode))
            {
                _translationState.SetLanguageCode(langCode);
                await JSRuntime.InvokeVoidAsync("setCookie","c-langCode", value, 30);

                StateHasChanged();
            }
        }
    }
}
