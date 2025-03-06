using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Backoffice.Web.Models.OddsEngine.Mapping;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class MappingProvider
    {
        [Parameter] public EventCallback<ProviderItemModel> OnDragCallback { get; set; }
        [Parameter] public List<ProviderItemModel> ProviderItems { get; set; } = new List<ProviderItemModel>
        {
            new ProviderItemModel { Id = 1, Name = "Test Fixture (ID#: 01)", Provider = ProviderType.SportsRadar },
            new ProviderItemModel { Id = 2, Name = "Test Fixture (ID#: 02)", Provider = ProviderType.BetGenius },
            new ProviderItemModel { Id = 3, Name = "Test Fixture (ID#: 03)", Provider = ProviderType.BetGenius },
        };
        private string _searchQuery { get; set; } = string.Empty;

        private List<ProviderItemModel> _filteredProviderItems { get; set; } = new List<ProviderItemModel>();
        private Dictionary<ProviderType, List<ProviderItemModel>> _groupProviderItems { get; set; } = new Dictionary<ProviderType, List<ProviderItemModel>>();

        private Dictionary<ProviderType, List<ProviderItemModel>> GetGroupedProviderItems()
        {
            return _filteredProviderItems.GroupBy(m => m.Provider).ToDictionary(g => g.Key, g => g.ToList());
        }

        protected override Task OnInitializedAsync()
        {
            _filteredProviderItems = ProviderItems;
            _groupProviderItems = GetGroupedProviderItems();
            return Task.CompletedTask;
        }

        public void FilterProviderItems(ChangeEventArgs e)
        {
            _searchQuery = e.Value.ToString();
            _filteredProviderItems = ProviderItems.Where(m => m.Name.ToLower().Contains(_searchQuery.ToLower())).ToList();
            _groupProviderItems = GetGroupedProviderItems();
        }

        private void HandleDragStart(ProviderItemModel selectedItem)
        {
            OnDragCallback.InvokeAsync(selectedItem);
        }

        private string GetProviderName(ProviderType providerType)
        {
            return providerType switch
            {
                ProviderType.SportsRadar => _translationState.GetValue("Sports Radar"),
                ProviderType.BetGenius => _translationState.GetValue("Bet Genius"),
                _ => _translationState.GetValue("Unknown Provider")
            };
        }
    }
}