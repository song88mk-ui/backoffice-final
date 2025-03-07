using Backoffice.Web.Models.OddsEngine.Mapping;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class MappingSetup
    {
        [Parameter] public ProviderItemModel DraggedProviderItem { get; set; }
        private Dictionary<ProviderType, ProviderItemModel> _droppedProviderItems = new Dictionary<ProviderType, ProviderItemModel>();

        private void HandleDropProviderItem()
        {
            if (DraggedProviderItem != null)
            {
                _droppedProviderItems[DraggedProviderItem.Provider] = DraggedProviderItem;
            }
        }

        private void HandleRemoveProviderItem(ProviderItemModel providerItem)
        {
            _droppedProviderItems.Remove(providerItem.Provider);
        }

        private string GetProviderAcronym(ProviderType providerType)
        {
            return providerType switch
            {
                ProviderType.SportsRadar => "SR",
                ProviderType.BetGenius => "BG",
                _ => "Unknown"
            };
        }

        private string GetProviderCss(ProviderType providerType)
        {
            return providerType switch
            {
                ProviderType.SportsRadar => "bg-danger",
                ProviderType.BetGenius => "bg-primary",
                _ => "Unknown"
            };
        }
    }
}
