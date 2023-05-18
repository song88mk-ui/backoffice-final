using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Backoffice.Web.Shared
{
    public partial class RedirectToPage : IDisposable
    {
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [Parameter] public string FromPage { get; set; }

        protected override async Task OnInitializedAsync()
        {
            if (UserModel != null)
            {
                var prefix = FromPage.Split('.').FirstOrDefault();
                if (prefix == null)
                    return;

                var claim = UserModel.Claims.FirstOrDefault(c => c.Type.Contains($"{prefix}."));
                if (claim != null)
                {
                    var menu = await MenuHelper.GetMenu(claim, FromPage);

                    NavigationManager.NavigateTo(menu.Url, true);
                }
            }
        }

        public void Dispose()
        {

        }
    }
}
