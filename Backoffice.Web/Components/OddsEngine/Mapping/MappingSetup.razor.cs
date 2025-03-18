using Backoffice.Web.Models.OddsEngine.Mapping;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class MappingSetup
    {
        [Parameter] public List<UnmappedModel> DraggedProviderItems { get; set; } = new List<UnmappedModel>();
        [Parameter] public EventCallback<MappingSetupItemModel> OnSubmitCallback { get; set; }

        private MappingSetupItemModel _mappingSetupItem;
        private EditContext _editContext;

        private Dictionary<int, UnmappedModel> _droppedProviderItems = new Dictionary<int, UnmappedModel>();

        protected override async Task OnInitializedAsync()
        {
            ResetValues();
            _editContext = new EditContext(_mappingSetupItem);
        }
    
        private void HandleSubmit()
        {
            OnSubmitCallback.InvokeAsync(_mappingSetupItem);
            ResetValues();
        }

        private void HandleDropProviderItem()
        {
            if (DraggedProviderItems == null) return;

            if (DraggedProviderItems.Count() > 1)
            {
                _mappingSetupItem.Providers = new List<UnmappedModel>(DraggedProviderItems);
            } else
            {
                int sameProviderIndex = _mappingSetupItem.Providers.FindIndex(x => x.ChTyId == DraggedProviderItems[0].ChTyId);
                bool hasItemOfSameProvider = sameProviderIndex != -1;

                if (hasItemOfSameProvider) { 
                    _mappingSetupItem.Providers[sameProviderIndex] = DraggedProviderItems[0];
                } else
                {
                    _mappingSetupItem.Providers.Add(DraggedProviderItems[0]);
                }
            }

            _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => _mappingSetupItem.Providers));
        }

        private void HandleRemoveProviderItem(UnmappedModel providerItem)
        {
            _mappingSetupItem.Providers = _mappingSetupItem.Providers.Where(item => item.ChTyId != providerItem.ChTyId).ToList();
            _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => _mappingSetupItem.Providers));
        }

        private void ResetValues()
        {
            _mappingSetupItem = new MappingSetupItemModel
            {
                Id = Guid.NewGuid().ToString(),
                Name = string.Empty,
                Providers = new List<UnmappedModel>()
            };
        }
    }
}
