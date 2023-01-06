using Backoffice.Shared.Models.Requests;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class Checkbox : ComponentBase
    {
        #region Properties

        [Parameter] public bool Checked { get; set; }
        [Parameter] public bool HasOnChange { get; set; }
        [Parameter] public object Param1 { get; set; }
        [Parameter] public EventCallback<CheckBoxModel> OnChangeCallback { get; set; }

        #endregion

        #region Method
        protected async Task OnChangeEvent(object checkedValue)
        {
            await OnChangeCallback.InvokeAsync(new CheckBoxModel
            {
                Checked = (bool)checkedValue,
                Param1 = Param1,
            });
        }
        #endregion
    }
}
