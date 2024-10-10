using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class Dropdown
    {
        #region Properties

        [Parameter] public List<SelectListItemWithCheckboxModel<string>> Items { get; set; }
        [Parameter] public EventCallback<string> OnClickEventCallback { get; set; }
        [Parameter] public string DropdownName { get; set; }
        [Parameter] public bool Multiple { get; set; }
        [Parameter] public string ImageClass { get; set; }
        [Parameter] public string DropdownClass { get; set; }

        #endregion

        #region Fields

        private bool _init = true;
        private bool _shouldRender;
        private int _selectedCount;
        private string _selectedName;
        private string _selectedValue;
        private LanguageCode _languageCode;
        private List<SelectListItemWithCheckboxModel<string>> _items;

        #endregion

        #region Method
        protected override async Task OnInitializedAsync()
        {
            _items = Items;

            _selectedCount = _items?.Count(c => c.Value != "all" && c.IsChecked) ?? 0;
            _selectedName = _selectedCount == 0 ? string.Empty : _items?.FirstOrDefault(c => c.Value != "all" && c.IsChecked)?.Name ?? string.Empty;
            _selectedValue = _selectedCount == 0 ? string.Empty : _items?.FirstOrDefault(c => c.Value != "all" && c.IsChecked)?.Value ?? string.Empty;

            if (Multiple)
            {
                if(!_items?.Any(i => i.Value == "all") ?? false)
                    _items.Insert(0, new SelectListItemWithCheckboxModel<string>
                    {
                        Value = "all",
                        Name = $"All {DropdownName}",
                        IsChecked = true
                    });
            }

        }

        protected override Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameters();

            if (_shouldRender)
                _languageCode = TranslationState.ActiveLanguageCode;

            _init = false;
            return base.OnParametersSetAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            _shouldRender = false;
        }

        protected override bool ShouldRender() => _shouldRender;

        public async Task OnCheckboxClickEvent(Dictionary<string, string> items)
        {
            _selectedName = string.Empty;
            _selectedValue = string.Empty;
            _selectedCount = items.Count;

            if (_selectedCount == 1)
            {
                _selectedName = items.FirstOrDefault().Value;
            }

            var selectedNames = string.Join(",", items.Select(s => s.Key));

            await OnClickEventCallback.InvokeAsync(selectedNames);
            _shouldRender = true;
        }


        public async Task OnCheckboxChangedEvent(SelectListItemWithCheckboxModel<string> item)
        {
            _selectedName = item.Name.ToString();
            _selectedValue = item.Value.ToString();

            await OnClickEventCallback.InvokeAsync(_selectedValue);
            _shouldRender = true;
        }

        #endregion

        #region Utilities

        private bool ValidateParameters()
        {
            return _init || _languageCode != TranslationState.ActiveLanguageCode;
        }

        #endregion
    }
}
