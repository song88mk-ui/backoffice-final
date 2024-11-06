using Backoffice.Shared.Enums;
using Backoffice.Web.ActionFilter;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Backoffice.Web.Pages
{
    public partial class ChooseAccess : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public UserState UserState { get; set; }



        public List<Claim> UserClaims { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            UserClaims = new List<Claim>();

            var cookieValue = await _jsRuntime.InvokeAsync<string>("getCookie", "c-langCode");
            await UpdateLanguageCode(cookieValue);

            await PageState.AuthenticateAccessEvent.InvokeAsync();

            var validClaimValues = new string[] { "1", "2", "0" };
            //var authState = await ((AuthStateProvider)AuthenticationStateProvider).GetAuthenticationStateAsync();
            //var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();

            if(UserState.ClaimsPrincipal != null)
                UserClaims.AddRange(UserState.ClaimsPrincipal?.Claims.Where(c => validClaimValues.Contains(c.Value)).ToList());
        }
        //protected override async Task OnAfterRenderAsync(bool firstRender)
        //{
        //    if (firstRender)
        //    {
        //        var cookieValue = await _jsRuntime.InvokeAsync<string>("getCookie", "c-langCode");
        //        await UpdateLanguageCode(cookieValue);  
        //    }
        //}
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
