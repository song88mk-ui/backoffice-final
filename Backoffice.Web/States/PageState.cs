using Backoffice.Shared.Enums;
using Backoffice.Web.Interfaces.Pages;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.States.Base;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.States
{
    public class PageState : BaseState<PageStateModel>
    {
        #region Fields

        private readonly IAuthenticationService _authenticationService;
        private readonly NavigationManager _navigationManager;

        #endregion

        #region Properties

        public string Region { get; set; } = string.Empty;
        public IPage Page { get; set; }

        #endregion

        #region Ctor

        public PageState(IAuthenticationService authenticationService, NavigationManager navigationManager) : base(new PageStateModel())
        {
            _authenticationService = authenticationService;
            _navigationManager = navigationManager;

            Page = Model.Page;
        }

        #endregion

        #region Methods

        public EventCallback AuthenticateAccessEvent => EventCallback.Factory.Create(this, AuthenticateAccess);

        #endregion

        #region Utilities

        private async Task AuthenticateAccess()
        {
            var authResponse = await _authenticationService.AuthenticateToken();

            if (authResponse.StatusCode != StatusCode.Success)
            {
                _navigationManager.NavigateTo("login", true);
            }
        }

        #endregion
    }
}
