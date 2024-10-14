using Backoffice.Shared.Enums;
using Backoffice.Web.ActionFilter;
using Backoffice.Web.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Backoffice.Web.Shared
{
    public partial class RedirectToPage : IDisposable
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        [Parameter] public string? FromPage { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var authState = await ((AuthStateProvider)AuthenticationStateProvider).GetAuthenticationStateAsync();
            var userClaims = authState.User.Claims.ToList();

            if (userClaims != null && userClaims.Any())
            {
                var prefix = FromPage?.Split('.').FirstOrDefault();

                if (prefix == null)
                    return;

                JSRuntime.InvokeVoidAsync("console.log", $"Redirection from {FromPage}");

                var claim = userClaims.FirstOrDefault(c => c.Type.Contains($"{prefix}."));

                if (claim != null)
                {
                    if (FromPage != null && FromPage == SportsbookClientPermission.SBC_SubUsers.ToString().Replace('_', '.'))
                    {
                        var subUserClaim = userClaims.FirstOrDefault(c => c.Type == FromPage);
                        JSRuntime.InvokeVoidAsync("console.log", $"Redirection claim {subUserClaim.Type} - {subUserClaim.Value}");
                    }

                    var menu = await MenuHelper.GetMenu(claim, FromPage ?? string.Empty);

                    NavigationManager.NavigateTo(menu.Url, true);
                }
            }
        }

        public void Dispose()
        {

        }
    }
}
