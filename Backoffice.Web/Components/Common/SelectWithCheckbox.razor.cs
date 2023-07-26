using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class SelectWithCheckbox
    {
        #region Properties

        [Parameter] public List<SelectListItemWithCheckboxModel<string>> Items { get; set; }
        [Parameter] public EventCallback<Dictionary<string, string>> OnCheckboxCallback { get; set; }
        [Parameter] public string AriaLabel { get; set; }
        [Parameter] public bool IsReadOnly { get; set; } = false;

        #endregion

        #region Methods

        public async Task CheckboxClicked(SelectListItemWithCheckboxModel<string> model)
        {
            if (model.IsChecked == false)
            {
                if (model.IsSelectAll || model.Value.ToUpper() == "ALL")
                {
                    Items.ForEach(x =>
                    {
                        x.IsChecked = true;
                    });
                }
                else
                {
                    model.IsChecked = true;

                    if (!Items.Where(c => c.IsSelectAll == false && c.Value != "all").Any(c => c.IsChecked == false))
                        Items[0].IsChecked = true;
                }
            }
            else
            {
                if (model.IsSelectAll || model.Value.ToUpper() == "ALL")
                {
                    Items.ForEach(x =>
                    {
                        x.IsChecked = false;
                    });
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
