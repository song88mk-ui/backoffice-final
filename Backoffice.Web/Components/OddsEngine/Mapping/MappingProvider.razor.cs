using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Backoffice.Shared.Models.Response;
using System.Diagnostics;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class MappingProvider
    {
        [Parameter] public EventCallback<List<UnmappedModel>> OnDragItemCallback { get; set; }
        [Parameter] public List<UnmappedModel> ProviderItems { get; set; } = new List<UnmappedModel>();

        private List<UnmappedModel> _filteredProviderItems { get; set; } = new List<UnmappedModel>();
        private List<UnmappedModel> _previousProviderItems = new List<UnmappedModel>();
        private Dictionary<int, List<UnmappedModel>> _groupProviderItems { get; set; } = new Dictionary<int, List<UnmappedModel>>();
        private Dictionary<int, string> providerIdToName = new Dictionary<int, string>
        {
            {
                0, "Sports Radar"
            },
            {
                1, "Bet Genius"
            },
        };

        private UnmappedModel _currentHoveredItem;

        private string _searchQuery { get; set; } = string.Empty;
        private bool _shouldRender;

        protected override bool ShouldRender() => _shouldRender;

        protected override void OnParametersSet()
        {
            _shouldRender = ValidateParameterRender();

            if (_shouldRender)
            {
                _filteredProviderItems = ProviderItems;
                _groupProviderItems = GetGroupedProviderItems();
                _previousProviderItems = new List<UnmappedModel>(ProviderItems);
            }
        }

        private Dictionary<int, List<UnmappedModel>> GetGroupedProviderItems()
        {
            return _filteredProviderItems.GroupBy(m => m.ChTyId).ToDictionary(g => g.Key, g => g.ToList());
        }

        public void FilterProviderItems(ChangeEventArgs e)
        {
            _searchQuery = e.Value.ToString();
            _filteredProviderItems = ProviderItems.Where(m => m.Na.ToLower().Contains(_searchQuery.ToLower())).ToList();
            _groupProviderItems = GetGroupedProviderItems();
        }

        private void HandleDragStart(UnmappedModel item)
        {
            var linkedItems = GetLinkedItems(item);

            OnDragItemCallback.InvokeAsync(linkedItems);
        }

        private void HandleMouseOver(UnmappedModel item)
        {
            _currentHoveredItem = item;
        }

        private void HandleMouseOut()
        {
            _currentHoveredItem = new UnmappedModel();
        }

        private string GetLinkedItemCss(UnmappedModel item)
        {
            var linkedItems = GetLinkedItems(item);

            _shouldRender = true;

            return _currentHoveredItem?.Na == item.Na ? "highlight" : "";
        }

        private List<UnmappedModel> GetLinkedItems(UnmappedModel item)
        {
            return ProviderItems.Where(x => x.Na.ToLower() == item.Na.ToLower()).DistinctBy(y => y.ChTyId).ToList();
        }

        private bool ValidateParameterRender()
        {
            return !ProviderItems.SequenceEqual(_previousProviderItems);
        }
    }
}     