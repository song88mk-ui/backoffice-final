using Backoffice.Web.Models.OddsEngine.Mapping;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class UnMapped
    {
        private ProviderItemModel _draggedProviderItem;

        private void HandleDragProviderItem(ProviderItemModel providerItem)
        {
            _draggedProviderItem = providerItem;
        }

        private void HandleDropProviderItem()
        {
            Console.WriteLine($"Dropped Provider Item: {_draggedProviderItem.Name}");
        }
    }
}
