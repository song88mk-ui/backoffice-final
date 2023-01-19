using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using RestSharp;

namespace Backoffice.Web.Components.ManageUser
{
    public partial class CreateOrUpdateModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        public UserRegisterRequest UserRegister { get; set; }
        #endregion

        protected override async Task OnInitializedAsync()
        {
            UserRegister = new UserRegisterRequest();
        }

        public async Task OnSaveClickedEvent()
        {
            UserRegister.AppId = (int)Application.Backoffice;
            UserRegister.AppName = Application.Backoffice.ToString();
        }
        public async Task OnStatusClickedEvent(bool isLocked)
        {
            if (isLocked)
                UserRegister.LockoutEnd = DateTime.Now;
            else
                UserRegister.LockoutEnd = null;
        }
        #region Methods
        public void Dispose()
        {
        }
        #endregion

        #region Utilities
        #endregion
    }
}
