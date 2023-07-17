using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Backoffice.Web.Shared
{
    public partial class RedirectToPage : IDisposable
    {
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
