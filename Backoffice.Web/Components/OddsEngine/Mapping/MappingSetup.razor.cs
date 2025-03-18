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
        [Parameter] public string FormId { get; set; } = "mappingSetupFormId";
        [Parameter] public bool HasSubmitButton { get; set; } = true;
        [Parameter] public MappingSetupItemModel MappingSetupItem { get; set; } = new MappingSetupItemModel();
        private MappingSetupItemModel _mappingSetupItem;
        private EditContext _editContext;
        private Dictionary<int, UnmappedModel> _droppedProviderItems = new Dictionary<int, UnmappedModel>();

        protected override async Task OnInitializedAsync()
        {
            ResetValues();
            _editContext = new EditContext(_mappingSetupItem);
        }

        protected override void OnParametersSet()
        {
            if (!string.IsNullOrEmpty(MappingSetupItem?.Id))
            {
                _mappingSetupItem = MappingSetupItem;
                _droppedProviderItems = _mappingSetupItem.Providers.ToDictionary(p => p.ChTyId);
                _editContext = new EditContext(_mappingSetupItem);
            }
        }
    
        private void HandleSubmit()
        {
            OnSubmitCallback.InvokeAsync(_mappingSetupItem);
            ResetValues();
        }

        private void HandleDropProviderItem()
        {
            if (DraggedProviderItems.Any())
            {
                //if (_droppedProviderItems.ContainsKey(DraggedProviderItem.ChTyId)) {
                //    HandleRemoveProviderItem(_droppedProviderItems[DraggedProviderItem.ChTyId]);
                //}
                //_droppedProviderItems[DraggedProviderItem.ChTyId] = DraggedProviderItem;

                _mappingSetupItem.Providers = new List<UnmappedModel>(DraggedProviderItems);
                _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => _mappingSetupItem.Providers));
            }
        }

        private void HandleRemoveProviderItem(UnmappedModel providerItem)
        {
            //_droppedProviderItems.Remove(providerItem.ChTyId);
            //_mappingSetupItem.Providers = _droppedProviderItems.Values.ToList();

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
