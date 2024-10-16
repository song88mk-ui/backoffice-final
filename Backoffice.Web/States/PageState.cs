using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.States
{
    public class PageState : BaseState<PageStateModel>
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly NavigationManager _navigationManager;

        public EventCallback AuthenticateAccessEvent => EventCallback.Factory.Create(this, AuthenticateAccess);

        public PageState(IAuthenticationService authenticationService, NavigationManager navigationManager) : base(new PageStateModel())
        {
            _authenticationService = authenticationService;
            _navigationManager = navigationManager;
        }

        public string Region { get; set; } = string.Empty;
        private async Task AuthenticateAccess()
        {
            var authResponse = await _authenticationService.AuthenticateToken();

            if (authResponse.StatusCode != StatusCode.Success)
            {
                _navigationManager.NavigateTo("login", true);
            }
        }
    }
}
