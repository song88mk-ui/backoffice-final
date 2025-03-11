using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class UnMapped
    {
        private ProviderMappingItem _currentMappingItem;
        private List<ProviderMappingItem> _currentMappedItems;
        private IEnumerable<IGrouping<int, UnmappedModel>> _sportsGroupedByProvider =
            new List<IGrouping<int, UnmappedModel>>();

        private bool _shouldRender;

        private Dictionary<int, string> providerIdToName = new Dictionary<int, string>
        {
            {
                1, "Bet Genius"
            },
            {
                2, "Sports Radar"
            }
        };

        protected override async Task OnInitializedAsync()
        {
            ResetMappingFields(true, true);

            await FetchUnmappedSports();

            _shouldRender = true;
        }

        protected override bool ShouldRender() => _shouldRender;

        private async Task FetchUnmappedSports()
        {
            var response = await _providerMappingService.GetUnmappedSports();
            _sportsGroupedByProvider = response.Result.Unmapped
                .GroupBy(x => x.ChTyId);
        }

        private void ResetMappingFields(bool resetSingle, bool resetList)
        {
            if (resetSingle)
            {
                _currentMappingItem = new ProviderMappingItem
                {
                    Id = "",
                    Na = "",
                    Info = new ProviderModel<string>
                    {
                        SrId = "0",
                        BgId = "0"
                    },
                    SpId = null,
                    SegId = null,
                    LeId = null
                };
            }

            if (resetList)
            {
                _currentMappedItems = new List<ProviderMappingItem>();
            }

            _shouldRender = true;
        }

        private ProviderMappingBulkRequest BuildRequestPayload(List<ProviderMappingItem> items)
        {
            var request = new ProviderMappingBulkRequest();

            foreach (var item in items)
            {
                request.Mapping.Add(
                    new ProviderMappingRequest()
                    {
                        Id = item.Id,
                        Na = item.Na,
                        Info = JsonSerializer.Serialize(item.Info),
                        SpId = item.SpId,
                        SegId = item.SegId,
                        LeId = item.LeId,
                        ChTyId = 0 // Will be changed to null since initial mappings must have no channel type id
                    }
                );
            }

            return request;
        }

        private void HandleCreateMappingClick()
        {
            _currentMappedItems.Add(_currentMappingItem);

            ResetMappingFields(true, false);

            _shouldRender = true;
        }

        private async Task HandleSaveMappingClick()
        {
            await _providerMappingService.AddOrUpdateProviderMappingBulk("sport", BuildRequestPayload(_currentMappedItems));

            ResetMappingFields(true, true);

            _shouldRender = true;
        }
    }
}