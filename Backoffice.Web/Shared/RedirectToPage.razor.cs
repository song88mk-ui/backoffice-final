using Backoffice.Shared.Enums;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using Newtonsoft.Json.Schema;
using System.Security.Claims;
using System.Text.Json;

namespace Backoffice.Web.Shared
{
    public partial class RedirectToPage : IDisposable
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [CascadingParameter(Name = "UserClaims")] public List<Claim>? UserClaims { get; set; }
        [Parameter] public string? FromPage { get; set; }

        protected override async Task OnInitializedAsync()
        {
            if (UserClaims != null && UserClaims.Any())
            {
                var prefix = FromPage?.Split('.').FirstOrDefault();

                if (prefix == null)
                    return;

                var claim = UserClaims.FirstOrDefault(c => c.Type.Contains($"{prefix}."));

                if (claim != null)
                {
                    if (FromPage != null && FromPage == SportsbookClientPermission.SBC_SubUsers.ToString().Replace('_', '.'))
                        await JSRuntime.InvokeVoidAsync("console.log", $"Redirection claim {JsonSerializer.Serialize(claim.Value)}");

                    var menu = await MenuHelper.GetMenu(claim, FromPage ?? string.Empty);

                    if (FromPage != null && FromPage == SportsbookClientPermission.SBC_SubUsers.ToString().Replace('_', '.'))
                        await JSRuntime.InvokeVoidAsync("console.log", $"Redirection url {menu.Url}");

                    NavigationManager.NavigateTo(menu.Url, true);
                }
            }
        }

        public void Dispose()
        {

        }
    }
}
