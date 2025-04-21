using Backoffice.Web.Pages.Base;
using Backoffice.Shared.Enums;

namespace Backoffice.Web.Pages.OddsEngine.Base
{
    public abstract class BaseMappingPage : BasePage
    {
        public Dictionary<MappingCategory, string> FetchMappingCategories = new Dictionary<MappingCategory, string>
        {
            { MappingCategory.Sport, "sports" },
            { MappingCategory.Segment, "segments" },
            { MappingCategory.League, "leagues" },
            { MappingCategory.Fixture, "fixtures" },
            { MappingCategory.Market, "markets" },
            { MappingCategory.Selection, "selections" }
        };
    }
}
