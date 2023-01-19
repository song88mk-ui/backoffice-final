using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class SelectWithCheckbox
    {
        #region Properties

        [Parameter] public bool IsShow { get; set; }
        [Parameter] public List<SelectListItemWithCheckboxModel<string>> Items { get; set; }
        [Parameter] public EventCallback<Dictionary<string, string>> OnCheckboxCallback { get; set; }

        #endregion

        #region Methods

        public async Task CheckboxClicked(SelectListItemWithCheckboxModel<string> model)
        {
            if (model.IsChecked == false)
            {
                if (model.Value == "all")
                {
                    foreach (var item in Items)
                    {
                        item.IsChecked = true;
                    }
                }
                else
                {
                    model.IsChecked = true;
                }
            }
            else
            {
                if (model.Value == "all")
                {
                    foreach (var item in Items)
                    {
                        item.IsChecked = false;
                    }
                }
                else
                {
                    model.IsChecked = false;
                    Items[0].IsChecked = false;
                }
            }

            var dictionary = Items.Where(c => c.Value != "all" && c.IsChecked).ToDictionary(c => c.Value, c => c.Name);
            await OnCheckboxCallback.InvokeAsync(dictionary);
        }

        #endregion
    }
}
