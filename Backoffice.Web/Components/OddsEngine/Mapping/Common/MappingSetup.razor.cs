using Backoffice.Web.Models.OddsEngine.Mapping;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Models;
using Backoffice.Shared.Enums;

namespace Backoffice.Web.Components.OddsEngine.Mapping.Common
{
    public partial class MappingSetup
    {
        [Parameter] public List<MappingProviderItemModel> MappingSetupProviderItems { get; set; }
        [Parameter] public EventCallback<MappingSetupItemModel> OnSubmitCallback { get; set; }
        [Parameter] public EventCallback<MappingProviderItemModel> OnRemoveProviderItemCallback { get; set; }
        [Parameter] public MappingSetupItemModel MappingSetupItem { get; set; } = new MappingSetupItemModel();
        [Parameter] public string FormId { get; set; } = "mappingSetupFormId";
        [Parameter] public bool HasSubmitButton { get; set; } = true;
        [Parameter] public bool IsEdit { get; set; } = false;
        [Parameter] public MappingCategory MappingCategory { get; set; }

        private MappingSetupItemModel _mappingSetupItem;
        private EditContext _editContext;

        private string _propertyNameToLabelItems;
        private bool _shouldRender;
        protected override bool ShouldRender() => _shouldRender;

        protected override async Task OnInitializedAsync()
        {
            ResetValues();
        }

        protected override async Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameterRender();
            if (!_shouldRender) return;

            SetDynamicProperties();

            if (!string.IsNullOrEmpty(MappingSetupItem.Id) && MappingSetupItem.Id != _mappingSetupItem.Id) {
                _mappingSetupItem = new MappingSetupItemModel
                {
                    Id = MappingSetupItem.Id,
                    Name = MappingSetupItem.Name,
                    Providers = MappingSetupItem.Providers.Select(s =>
                        new MappingProviderItemModel
                        {
                            SpId = s.SpId,
                            SpNa = s.SpNa,
                            SegId = s.SegId,
                            SegNa = s.SegNa,
                            LeId = s.LeId,
                            LeNa = s.LeNa,
                            FiId = s.FiId,
                            FiNa = s.FiNa,
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

        private void SetDynamicProperties()
        {
            switch (MappingCategory)
            {
                case MappingCategory.Sport:
                    _propertyNameToLabelItems = "Sp";
                    break;
                case MappingCategory.Segment:
                    _propertyNameToLabelItems = "Seg";
                    break;
                case MappingCategory.League:
                    _propertyNameToLabelItems = "Le";
                    break;
                case MappingCategory.Fixture:
                    _propertyNameToLabelItems = "Fi";
                    break;
                default:
                    _propertyNameToLabelItems = "Sp";
                    break;
            }
        }

        private async Task HandleSubmit()
        {
            await OnSubmitCallback.InvokeAsync(_mappingSetupItem);

            if (!IsEdit)
            {
                ResetValues();
            }
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

        private bool IsDisabledCreateBtn()
        {
            return string.IsNullOrWhiteSpace(_mappingSetupItem.Name) || !_mappingSetupItem.Providers.Any();
        }

        private string GetDisabledCreateBtnCss()
        {
            return IsDisabledCreateBtn() ? "disabled" : "";
        }

        private void OnNameInputChange(ChangeEventArgs e)
        {
            _mappingSetupItem.Name = e.Value.ToString();

            _shouldRender = true;
        }

        private string GetItemLabel(MappingProviderItemModel item)
        {
            var idPropertyInfo = typeof(MappingProviderItemModel).GetProperty($"{_propertyNameToLabelItems}Id");
            var labelPropertyInfo = typeof(MappingProviderItemModel).GetProperty($"{_propertyNameToLabelItems}Na");

            return $"{labelPropertyInfo.GetValue(item, null)} (#{idPropertyInfo.GetValue(item, null)})";
        }

        private bool ValidateParameterRender()
        {
            return !MappingSetupProviderItems.SequenceEqual(_mappingSetupItem.Providers) || !string.IsNullOrEmpty(MappingSetupItem?.Id);
        }
    }
}