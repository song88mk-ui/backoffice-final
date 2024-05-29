using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using System.Security.Claims;

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
            await JSRuntime.InvokeAsync<string>("console.log", "Redirection Triggered");

            if (UserClaims != null && UserClaims.Any())
            {
                var prefix = FromPage?.Split('.').FirstOrDefault();


                await JSRuntime.InvokeAsync<string>("console.log", $"Redirection prefix {prefix}");
                if (prefix == null)
                    return;

                var claim = UserClaims.FirstOrDefault(c => c.Type.Contains($"{prefix}."));
                if (claim != null)
                {
                    await JSRuntime.InvokeAsync<string>("console.log", $"Redirection prefix {prefix}");
                    var menu = await MenuHelper.GetMenu(claim, FromPage ?? string.Empty);


                    await JSRuntime.InvokeAsync<string>("console.log", $"Redirection url {menu.Url}");
                    NavigationManager.NavigateTo(menu.Url, true);
                }
            }
        }

        public void Dispose()
        {

        }
    }
}
