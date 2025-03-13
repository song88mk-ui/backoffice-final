using Backoffice.Web.Models.OddsEngine.Mapping;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class MappingSetup
    {
        [Parameter] public UnmappedModel DraggedProviderItem { get; set; } = new UnmappedModel();
        [Parameter] public EventCallback<MappingSetupModel> OnSubmitCallback { get; set; }
        private MappingSetupModel _mappingSetup = new MappingSetupModel();
        private Dictionary<int, UnmappedModel> _droppedProviderItems = new Dictionary<int, UnmappedModel>();
        private EditContext _editContext;
        private Dictionary<int, string> providerAcronym = new Dictionary<int, string>
        {
            {
                1, "BG"
            },
            {
                2, "SR"
            }
        };

        private Dictionary<int, string> providerCss = new Dictionary<int, string>
        {
            {
                1, "bg-primary"
            },
            {
                2, "bg-danger"
            }
        };

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
                _droppedProviderItems[DraggedProviderItem.ChTyId] = DraggedProviderItem;
                _mappingSetup.Providers = _droppedProviderItems.Values.ToList();
                _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => _mappingSetup.Providers));
            }
        }

        private void HandleRemoveProviderItem(UnmappedModel providerItem)
        {
            _droppedProviderItems.Remove(providerItem.ChTyId);
            _mappingSetup.Providers = _droppedProviderItems.Values.ToList();
            _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => _mappingSetup.Providers));
        }
    }
}
