using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Backoffice.Web.Components.Common
{
    public partial class SelectWithSearch
    {
        #region Properties
        [Parameter] public List<SelectListItemModel<long>> Items { get; set; }
        [Parameter] public EventCallback<SelectListItemModel<long>> OnSelectionCallback { get; set; }
        [Parameter] public string AriaLabel { get; set; }
        [Parameter] public bool IsReadOnly { get; set; } = false;
        [Parameter] public SelectListItemModel<long> SelectedItem { get; set; }
        [Parameter] public ConfigState State { get; set; }
        public string SearchText = "";
        #endregion

        #region Methods
        public async Task OnSearchTextChanged(ChangeEventArgs e)
        {
            SearchText = e.Value.ToString();
        }
        public async Task OnSelectTemplateClickedEvent(SelectListItemModel<long> e)
        {
            SelectedItem = e;
            OnSelectionCallback.InvokeAsync(SelectedItem);
        }
        public async Task OnRemoveTemplateClickedEvent()
        {
            SelectedItem = new SelectListItemModel<long>()
            {
                Value = -2,
                Name = "Remove Current Template"
            };
            OnSelectionCallback.InvokeAsync(SelectedItem);
        }
        #endregion
    }
}
