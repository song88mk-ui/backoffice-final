using Backoffice.Web.ActionFilter;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Backoffice.Web.Pages
{
    public partial class ChooseAccess : IDisposable
    {
        #region Properties

        [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }

        public List<Claim> UserClaims { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            UserClaims = new List<Claim>();

            await AuthenticateAccess.InvokeAsync();

            var validClaimValues = new string[] { "1", "2", "0" };
            var authState = await ((AuthStateProvider)AuthenticationStateProvider).GetAuthenticationStateAsync();
            //var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();

            UserClaims.AddRange(authState.User.Claims.Where(c => validClaimValues.Contains(c.Value)).ToList());
        }

        public async Task OnLogoutClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");
            parameters.Add("ModalHeaderClass", "text-center");
            parameters.Add("TitleClass", "w-100");

            var options = new ModalOptions();
            ModalService.Open<ConfirmLogoutModal>("Logout", parameters, options);
        }

        public void Dispose()
        {
            //AuthenticationStateProvider = null;
            //UserClaims = null;

            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
