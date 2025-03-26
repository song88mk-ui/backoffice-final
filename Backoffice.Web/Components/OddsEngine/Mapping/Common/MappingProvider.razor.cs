using Backoffice.Web.Models.OddsEngine.Mapping;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Mapping.Common
{
    public partial class MappingProvider
    {
        [Parameter] public EventCallback<MappingProviderItemModel> OnClickItemCallback { get; set; }
        [Parameter] public List<MappingProviderItemModel> ProviderItems { get; set; }
        [Parameter] public List<MappingProviderItemModel> MappingSetupProviderItems { get; set; }

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
        private bool _shouldRender;

        protected override bool ShouldRender() => _shouldRender;

        protected override void OnParametersSet()
        {
            _shouldRender = ValidateParameterRender();

            if (_shouldRender)
            {
                _mappingSetupProviderItems = MappingSetupProviderItems;
                _filteredProviderItems = ProviderItems;
                _groupProviderItems = GetGroupedProviderItems();
                _previousProviderItems = new List<MappingProviderItemModel>(_filteredProviderItems);
            }
        }

        private Dictionary<int, List<MappingProviderItemModel>> GetGroupedProviderItems()
        {
            return _filteredProviderItems.GroupBy(m => m.ChTyId).ToDictionary(g => g.Key, g => g.OrderBy(x => x.IsCurrentLinkedItem ? 0 : 1).ToList());
        }

        public void FilterProviderItems(ChangeEventArgs e)
        {
            _searchQuery = e.Value.ToString();
            _filteredProviderItems = _filteredProviderItems.Where(m => m.Na.ToLower().Contains(_searchQuery.ToLower())).ToList();
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

        private bool ValidateParameterRender()
        {
            return !ProviderItems.SequenceEqual(_previousProviderItems) || !MappingSetupProviderItems.SequenceEqual(_mappingSetupProviderItems);
        }
    }
}     