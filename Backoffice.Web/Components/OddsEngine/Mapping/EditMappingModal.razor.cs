using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Mapping;
using Backoffice.Web.Services;
using Backoffice.Web.Components.Common;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class EditMappingModal: IDisposable
    {
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter] private CustomModal Modal { get; set; }
        private List<UnmappedModel> _draggedProviderItems;
        private List<MappingProviderItemModel> _unmappedSports;
        private string _formId = "editMappingFormId";
        private bool _isPreLoader = true;
        private MappingSetupItemModel _mappingSetupItem = new MappingSetupItemModel();
        private Dictionary<MappingCategory, string> _mappingCategories = new Dictionary<MappingCategory, string>
        {
            { MappingCategory.Sport, "sport" },
            { MappingCategory.Segment, "segment" },
            { MappingCategory.League, "league" },
            { MappingCategory.Fixture, "fixture" },
            { MappingCategory.Market, "market" },
            { MappingCategory.Selection, "selection" }
        };

        protected override async Task OnInitializedAsync()
        {
            var response = await _providerMappingService.GetUnmappedSports();
            _unmappedSports = response.Result.Unmapped.Select(s => new MappingProviderItemModel(s)).ToList();

            var mappedResponse = await _providerMappingService.GetSportMappingBySpId(ModalParameters.TryGet<int>("SpId"));
            var sportMapping = mappedResponse.Result;

            _mappingSetupItem = new MappingSetupItemModel
            {
                Id = sportMapping.SpId.ToString(),
                Name = sportMapping.SpNa,
                Providers = GetUnmappedProviderByInfo(sportMapping.Info)
            };

            _isPreLoader = false;
        }

        private List<MappingProviderItemModel> GetUnmappedProviderByInfo(ProviderModel<int> info)
        {
            var providers = new List<MappingProviderItemModel>();

            foreach (var provider in _unmappedSports)
            {
                if ((provider.Id == info.SrId.ToString() || provider.Id == info.BgId.ToString()) && provider.Id != "0")
                {
                    providers.Add(provider);
                }
            }

            return providers;
        }

        public void OnCloseClickEvent()
        {
            ModalService.Cancel();
        }

        public void OnSaveClickEvent()
        {
            ModalService.Cancel();
        }
        public void Dispose()
        {
        }
    }
}
