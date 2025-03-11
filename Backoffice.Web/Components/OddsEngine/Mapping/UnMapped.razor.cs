using System.Collections;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;

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

        private void HandleCreateMappingClick()
        {
            ResetMappingFields(true, false);

            _shouldRender = true;
        }

        private async Task HandleSaveMappingClick()
        {
            ResetMappingFields(true, true);

            _shouldRender = true;
        }
    }
}