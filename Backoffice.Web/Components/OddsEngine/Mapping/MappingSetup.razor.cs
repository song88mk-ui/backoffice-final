using Backoffice.Web.Models.OddsEngine.Mapping;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class MappingSetup
    {
        [Parameter] public List<UnmappedModel> MappingSetupProviderItems { get; set; }
        [Parameter] public EventCallback<MappingSetupItemModel> OnSubmitCallback { get; set; }
        [Parameter] public EventCallback<UnmappedModel> OnRemoveProviderItemCallback { get; set; }

        private MappingSetupItemModel _mappingSetupItem;
        private EditContext _editContext;

        private bool _shouldRender;

        protected override bool ShouldRender() => _shouldRender;

        protected override async Task OnInitializedAsync()
        {
            ResetValues();
        }

        protected override async Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameterRender();

            if (_shouldRender) {
                _mappingSetupItem.Providers = new List<UnmappedModel>(MappingSetupProviderItems);

                _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => _mappingSetupItem.Providers));
            }
        }
    
        private void HandleSubmit()
        {
            OnSubmitCallback.InvokeAsync(_mappingSetupItem);
            ResetValues();
        }

        private void HandleRemoveProviderItemClick(UnmappedModel providerItem)
        {
            OnRemoveProviderItemCallback.InvokeAsync(providerItem);

            _shouldRender = true;
        }

        private void ResetValues()
        {
            _mappingSetupItem = new MappingSetupItemModel
            {
                Id = Guid.NewGuid().ToString(),
                Name = string.Empty,
                Providers = new List<UnmappedModel>()
            };

            _editContext = new EditContext(_mappingSetupItem);

            _shouldRender = true;
        }

        private bool ValidateParameterRender()
        {
            return !MappingSetupProviderItems.SequenceEqual(_mappingSetupItem.Providers);
        }
    }
}
