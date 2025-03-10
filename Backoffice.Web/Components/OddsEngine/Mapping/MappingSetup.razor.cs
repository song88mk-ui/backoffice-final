using Backoffice.Web.Models.OddsEngine.Mapping;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class MappingSetup
    {
        [Parameter] public ProviderItemModel DraggedProviderItem { get; set; } = new ProviderItemModel();
        [Parameter] public EventCallback<MappingSetupModel> OnSubmitCallback { get; set; }
        private MappingSetupModel _mappingSetup = new MappingSetupModel();
        private Dictionary<ProviderType, ProviderItemModel> _droppedProviderItems = new Dictionary<ProviderType, ProviderItemModel>();
        private EditContext _editContext;

        protected override void OnInitialized()
        {
            _editContext = new EditContext(_mappingSetup);
        }
    
        private void HandleSubmit()
        {
            OnSubmitCallback.InvokeAsync(_mappingSetup);
        }
        private void HandleDropProviderItem()
        {
            if (DraggedProviderItem != null)
            {
                _droppedProviderItems[DraggedProviderItem.Provider] = DraggedProviderItem;
                _mappingSetup.Providers = _droppedProviderItems.Values.ToList();
                _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => _mappingSetup.Providers));
            }
        }

        private void HandleRemoveProviderItem(ProviderItemModel providerItem)
        {
            _droppedProviderItems.Remove(providerItem.Provider);
            _mappingSetup.Providers = _droppedProviderItems.Values.ToList();
            _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => _mappingSetup.Providers));
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
