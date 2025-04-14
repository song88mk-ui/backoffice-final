using Backoffice.Shared.Enums;
using Backoffice.Web.Models.OddsEngine.Mapping;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Mapping.Common
{
    public partial class MappingProvider
    {
        [Parameter] 
        public EventCallback<MappingProviderItemModel> OnClickItemCallback { get; set; }
        [Parameter] 
        public List<MappingProviderItemModel> ProviderItems { get; set; }
        [Parameter] 
        public List<MappingProviderItemModel> MappingSetupProviderItems { get; set; }
        [Parameter] 
        public MappingCategory MappingCategory { get; set; }
        [Parameter] 
        public Dictionary<MappingCategory, MappingDropdownFilterGroup> DropdownFilters { get; set; }
        [Parameter] 
        public EventCallback<DropdownFilterCallbackModel> OnDropdownItemClickCallback { get; set; }

        private List<MappingProviderItemModel> _filteredProviderItems { get; set; } = new List<MappingProviderItemModel>();
        private List<MappingProviderItemModel> _previousProviderItems = new List<MappingProviderItemModel>();
        private List<MappingProviderItemModel> _mappingSetupProviderItems = new List<MappingProviderItemModel>();

        private Dictionary<int, List<MappingProviderItemModel>> _groupProviderItems { get; set; } = new Dictionary<int, List<MappingProviderItemModel>>();
        private Dictionary<int, string> providerIdToName = new Dictionary<int, string>
        {
            {
                0, "Sports Radar"
            },
            {
                1, "Bet Genius"
            },
        };

        private string _searchQuery { get; set; } = string.Empty;
        private string _propertyNameToSortBy;
        private string _propertyNameToSearchBy;
        private string _propertyNameToLabelItems;
        private bool _showSportsDropdown;
        private bool _showSegmentsDropdown;
        private bool _showLeaguesDropdown;
        private bool _shouldRender;

        protected override bool ShouldRender() => _shouldRender;

        protected override void OnParametersSet()
        {
            _shouldRender = ValidateParameterRender();

            if (_shouldRender)
            {
                _mappingSetupProviderItems = MappingSetupProviderItems;
                _filteredProviderItems = ProviderItems;
                
                SetDynamicProperties();

                _groupProviderItems = GetGroupedProviderItems();
                _previousProviderItems = new List<MappingProviderItemModel>(_filteredProviderItems);
            }
        }

        private void SetDynamicProperties()
        {
            switch (MappingCategory)
            {
                case MappingCategory.Sport:
                    _propertyNameToSortBy = "SpNa";
                    _propertyNameToSearchBy = "SpNa";
                    _propertyNameToLabelItems = "Sp";
                    _showSportsDropdown = false;
                    _showSegmentsDropdown = false;
                    _showLeaguesDropdown = false;
                    break;
                case MappingCategory.Segment:
                    _propertyNameToSortBy = "SegNa";
                    _propertyNameToSearchBy = "SegNa";
                    _propertyNameToLabelItems = "Seg";
                    _showSportsDropdown = true;
                    _showSegmentsDropdown = false;
                    _showLeaguesDropdown = false;
                    break;
                case MappingCategory.League:
                    _propertyNameToSortBy = "LeNa";
                    _propertyNameToSearchBy = "LeNa";
                    _propertyNameToLabelItems = "Le";
                    _showSportsDropdown = true;
                    _showSegmentsDropdown = true;
                    _showLeaguesDropdown = false;
                    break;
                case MappingCategory.Fixture:
                    _propertyNameToSortBy = "FiNa";
                    _propertyNameToSearchBy = "FiNa";
                    _propertyNameToLabelItems = "Fi";
                    _showSportsDropdown = true;
                    _showSegmentsDropdown = true;
                    _showLeaguesDropdown = true;
                    break;
                default:
                    _propertyNameToSortBy = "SpNa";
                    _propertyNameToSearchBy = "SpNa";
                    _propertyNameToLabelItems = "Sp";
                    _showSportsDropdown = false;
                    _showSegmentsDropdown = false;
                    _showLeaguesDropdown = false;
                    break;
            }
        }

        private Dictionary<int, List<MappingProviderItemModel>> GetGroupedProviderItems()
        {
            var propertyInfo = typeof(MappingProviderItemModel).GetProperty(_propertyNameToSortBy);

            return _filteredProviderItems.GroupBy(m => m.ChTyId).ToDictionary(g => g.Key, g => g.OrderBy(x => propertyInfo.GetValue(x, null)).OrderBy(x => x.IsCurrentLinkedItem ? 0 : 1).ToList());
        }

        public void FilterProviderItems(ChangeEventArgs e)
        {
            var propertyInfo = typeof(MappingProviderItemModel).GetProperty(_propertyNameToSearchBy);

            _searchQuery = e.Value.ToString();
            _filteredProviderItems = ProviderItems.Where(m => propertyInfo.GetValue(m, null).ToString().ToLower().Contains(_searchQuery.ToLower())).ToList();
            _groupProviderItems = GetGroupedProviderItems();
        }

        public void HandleItemClick(MappingProviderItemModel item)
        {
            OnClickItemCallback.InvokeAsync(item);
        }

        private string GetLinkedItemCss(MappingProviderItemModel item)
        {
            return item.IsCurrentLinkedItem ? "highlight" : "";
        }

        private string GetItemLabel(MappingProviderItemModel item)
        {
            var idPropertyInfo = typeof(MappingProviderItemModel).GetProperty($"{_propertyNameToLabelItems}Id");
            var labelPropertyInfo = typeof(MappingProviderItemModel).GetProperty($"{_propertyNameToLabelItems}Na");

            return $"{labelPropertyInfo.GetValue(item, null)} (#{idPropertyInfo.GetValue(item, null)})";
        }

        private bool ValidateParameterRender()
        {
            return !ProviderItems.SequenceEqual(_previousProviderItems) || !MappingSetupProviderItems.SequenceEqual(_mappingSetupProviderItems);
        }
    }
}     