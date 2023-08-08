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
        [Parameter] public SelectListItemModel<long>? SelectedItem { get; set; }
        [Parameter] public ConfigState State { get; set; }
        public string SearchText = "";
        private SelectListItemModel<long>? _selectedItem;
        private bool _hasDefault = false;
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            _hasDefault = SelectedItem != null;
            await Task.CompletedTask;
        }
        public async Task OnSearchTextChanged(ChangeEventArgs e)
        {
            SearchText = e.Value?.ToString() ?? string.Empty;
        }
        public async Task OnSelectTemplateClickedEvent(SelectListItemModel<long> e)
        {
            if (SelectedItem == null)
                _selectedItem = e;
            else if (SelectedItem.Value != e.Value && SelectedItem.Name != SelectedItem.Name)
                _selectedItem = e;

            SelectedItem = new SelectListItemModel<long>()
            {
                Value = e.Value,
                Name = e.Name,
            };
            await OnSelectionCallback.InvokeAsync(SelectedItem);
        }
        public async Task OnRemoveTemplateClickedEvent()
        {
            SelectedItem = new SelectListItemModel<long>()
            {
                Value = -1,
                Name = "Remove Current Template"
            };
            _selectedItem = new SelectListItemModel<long>()
            {
                Value = -1,
                Name = "Remove Current Template"
            };

            await OnSelectionCallback.InvokeAsync(SelectedItem);
        }
        private async Task RemoveSelectedItem()
        {
            _selectedItem = null;
            SelectedItem = null;
            await OnSelectionCallback.InvokeAsync(SelectedItem);
        }
        #endregion
    }
}
