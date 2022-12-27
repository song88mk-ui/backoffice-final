using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Backoffice.Web.Models;
using System.Security.Claims;

namespace Backoffice.Web.Pages
{
    public partial class ChooseAccess : IDisposable
    {
        [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        public List<Claim> UserClaims { get; set; }

        protected override async Task OnInitializedAsync()
        { 
            var validClaimValues = new string[] { "1", "2", "0" };

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            UserClaims = new List<Claim>();
            UserClaims.AddRange(authState.User.Claims.Where(x => validClaimValues.Contains(x.Value)).ToList());
        }
        public void Dispose()
        {
        }
    }
}
