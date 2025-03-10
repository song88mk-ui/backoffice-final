using System.Collections;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class UnMapped
    {
        private Dictionary<int, string> providerIdToName = new Dictionary<int, string>
        {
            {
                1, "Bet Genius"
            },
            {
                2, "Sports Radar"
            }
        };

        private List<UnmappedModel> _unMappedSports = new();


        private IEnumerable<IGrouping<int, UnmappedModel>> _sportsGroupedByProvider =
            new List<IGrouping<int, UnmappedModel>>();

        protected override async Task OnInitializedAsync()
        {
            var response = await _providerMappingService.GetUnmappedSports();
            _sportsGroupedByProvider = response.Result.Unmapped
                .GroupBy(x => x.ChTyId);
        }
    }
}