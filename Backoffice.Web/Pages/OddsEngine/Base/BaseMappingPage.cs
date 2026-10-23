using Backoffice.Web.Pages.Base;
using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.States;
using Backoffice.Web.Models.OddsEngine.Mapping;

namespace Backoffice.Web.Pages.OddsEngine.Base
{
    public abstract class BaseMappingPage : BasePage
    {
        public List<BreadcrumbModel> BreadcrumbModel { get; set; }
        public new PageHeaderModel PageHeaderModel { get; set; }
        public MappingTab ActiveTab { get; set; }
        public string PageHeaderTitle { get; set; }
        public string BreadcrumbsMappingLabel { get; set; }
        public string BreadcrumbsCurrentPageLabel { get; set; }
        public int UnmappedCount { get; set; }
        public int MappedCount { get; set; }
        public bool RenderComponent { get; set; }

        public Dictionary<MappingCategory, string> FetchMappingCategories = new Dictionary<MappingCategory, string>
        {
            { MappingCategory.Sport, "sports" },
            { MappingCategory.Segment, "segments" },
            { MappingCategory.League, "leagues" },
            { MappingCategory.Fixture, "fixtures" },
            { MappingCategory.Market, "markets" },
            { MappingCategory.Selection, "selections" }
        };

        protected override bool ShouldRender() => RenderComponent;

        public void SetBreadcrumbsAndHeader()
        {
            PageHeaderModel = new PageHeaderModel()
            {
                Title = PageHeaderTitle,
                ShowLanguageDropDown = false
            };

            BreadcrumbModel = new List<BreadcrumbModel>
            {
                new BreadcrumbModel
                {
                    Name = BreadcrumbsMappingLabel,
                    Url = string.Empty,
                    IsCurrentPage = false
                },
                new BreadcrumbModel
                {
                    Name = BreadcrumbsCurrentPageLabel,
                    Url = string.Empty,
                    IsCurrentPage = true
                }
            };

            ActiveTab = MappingTab.Unmapped;

            RenderComponent = true;
        }

        public void HandleTabClick(MappingTab tab)
        {
            ActiveTab = tab;
            RenderComponent = true;
        }
    }
}
