using Backoffice.Web.Interfaces.Pages;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.Base
{
    public abstract class BasePage : ComponentBase, IPage
    {
        public PageHeaderModel PageHeaderModel { get; set; }

        public async Task RefreshPage()
        {
            // add custom code here or any validations that you need
            await InvokeAsync(StateHasChanged);
        }
    }
}
