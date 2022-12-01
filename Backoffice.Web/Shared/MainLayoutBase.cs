using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace Backoffice.Web.Shared
{
    public class MainLayoutBase : LayoutComponentBase, IDisposable
    {
        [Inject] public IConfiguration Configuration { get; set; }
        [Inject] public IHttpContextAccessor HttpContextAccessor { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

        public UserModel Model { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                if (HttpContextAccessor.HttpContext.Session.GetString("UserToken") == null)
                {
                    NavigationManager.NavigateTo("/login", true);
                }
            }
            catch (Exception)
            {
                NavigationManager.NavigateTo("/login", true);
            }
        }

        public async void Dispose()
        {
        }
    }
}
