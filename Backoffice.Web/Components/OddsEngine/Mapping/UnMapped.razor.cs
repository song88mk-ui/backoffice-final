using Backoffice.Web.Models.OddsEngine.Mapping;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class UnMapped
    {
        private ProviderItemModel _draggedProviderItem;

        public List<ProviderItemModel> _providerItems { get; set; } = new List<ProviderItemModel>
        {
            new ProviderItemModel { Id = 1, Name = "Test Fixture (ID#: 01)", Provider = ProviderType.SportsRadar },
            new ProviderItemModel { Id = 2, Name = "Test Fixture (ID#: 02)", Provider = ProviderType.BetGenius },
            new ProviderItemModel { Id = 3, Name = "Test Fixture (ID#: 03)", Provider = ProviderType.BetGenius },
        };

        private void HandleDragProviderItem(ProviderItemModel providerItem)
        {
            _draggedProviderItem = providerItem;
        }
    }
}
