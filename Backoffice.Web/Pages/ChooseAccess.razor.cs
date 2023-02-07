using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Backoffice.Web.Pages
{
    public partial class ChooseAccess : IDisposable
    {
        [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        public List<Claim> UserClaims { get; set; }

        protected override async Task OnInitializedAsync()
        {
            UserClaims = new List<Claim>();

            var validClaimValues = new string[] { "1", "2", "0" };
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();

            UserClaims.AddRange(authState.User.Claims.Where(c => validClaimValues.Contains(c.Value)).ToList());
        }

        public void Dispose()
        {
            AuthenticationStateProvider = null;
            UserClaims = null;

            // Suppress finalization.
            GC.SuppressFinalize(this);
        }
    }
}
