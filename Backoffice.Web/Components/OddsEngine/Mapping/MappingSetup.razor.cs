using Backoffice.Web.Models.OddsEngine.Mapping;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Models;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class MappingSetup
    {
        [Parameter] public List<MappingProviderItemModel> MappingSetupProviderItems { get; set; }
        [Parameter] public EventCallback<MappingSetupItemModel> OnSubmitCallback { get; set; }
        [Parameter] public EventCallback<MappingProviderItemModel> OnRemoveProviderItemCallback { get; set; }
        [Parameter] public MappingSetupItemModel MappingSetupItem { get; set; } = new MappingSetupItemModel();
        [Parameter] public string FormId { get; set; } = "mappingSetupFormId";
        [Parameter] public bool HasSubmitButton { get; set; } = true;

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
                if (MappingSetupItem?.Id != null && MappingSetupItem?.Name != null && MappingSetupItem?.Providers != null) {
                    _mappingSetupItem = new MappingSetupItemModel
                    {
                        Id = MappingSetupItem.Id,
                        Name = MappingSetupItem.Name,
                        Providers = MappingSetupItem.Providers.Select(s =>
                            new MappingProviderItemModel
                            {
                                Id = s.Id,
                                Na = s.Na,
                                ChTyId = s.ChTyId,
                                OtPrLi = new ProviderModel<List<int>>
                                {
                                    SrId = s.OtPrLi.SrId,
                                    BgId = s.OtPrLi.BgId
                                },
                                IsCurrentLinkedItem = s.IsCurrentLinkedItem,
                                HasLinkedItem = s.HasLinkedItem,
                            }).ToList()
                    };
                }
                
                _mappingSetupItem.Providers = new List<MappingProviderItemModel>(MappingSetupProviderItems);

                _editContext = new EditContext(_mappingSetupItem);

                _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => _mappingSetupItem.Providers));
            }
        }
    
        private void HandleSubmit()
        {
            OnSubmitCallback.InvokeAsync(_mappingSetupItem);
            ResetValues();
        }

        private void HandleRemoveProviderItemClick(MappingProviderItemModel providerItem)
        {
            OnRemoveProviderItemCallback.InvokeAsync(providerItem);

            _shouldRender = true;
        }

        private void ResetValues()
        {
            _mappingSetupItem = new MappingSetupItemModel
            {
                Id = "0",
                Name = string.Empty,
                Providers = new List<MappingProviderItemModel>()
            };

            _editContext = new EditContext(_mappingSetupItem);

            _shouldRender = true;
        }

        private bool ValidateParameterRender()
        {
            return !MappingSetupProviderItems.SequenceEqual(_mappingSetupItem.Providers) || !string.IsNullOrEmpty(MappingSetupItem?.Id);
        }
    }
}