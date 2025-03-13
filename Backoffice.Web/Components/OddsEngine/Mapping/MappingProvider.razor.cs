using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class MappingProvider
    {
        [Parameter] public EventCallback<UnmappedModel> OnDragCallback { get; set; }
        [Parameter] public List<UnmappedModel> ProviderItems { get; set; } = new List<UnmappedModel>();

        private List<UnmappedModel> _filteredProviderItems { get; set; } = new List<UnmappedModel>();
        private List<UnmappedModel> _previousProviderItems = new List<UnmappedModel>();
        private Dictionary<int, List<UnmappedModel>> _groupProviderItems { get; set; } = new Dictionary<int, List<UnmappedModel>>();
        private Dictionary<int, string> providerIdToName = new Dictionary<int, string>
        {
            {
                1, "Bet Genius"
            },
            {
                2, "Sports Radar"
            }
        };

        private string _searchQuery { get; set; } = string.Empty;
        private bool _shouldRender;

        protected override bool ShouldRender() => _shouldRender;

        protected override void OnParametersSet()
        {
            _shouldRender = ValidateRender();

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

        private void HandleDragStart(UnmappedModel selectedItem)
        {
            OnDragCallback.InvokeAsync(selectedItem);
        }

        private bool ValidateRender()
        {
            return !ProviderItems.SequenceEqual(_previousProviderItems);
        }
    }
}     