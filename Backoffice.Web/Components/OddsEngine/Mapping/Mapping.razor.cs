using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Mapping;

public partial class Mapping : ComponentBase
{
    [Parameter]
    public required RenderFragment Unmapped { get; set; }

    [Parameter]
    public required RenderFragment Mapped { get; set; }

    private List<BreadcrumbModel> _breadcrumbs = new List<BreadcrumbModel>
    {
        new BreadcrumbModel
        {
            Name = "Mapping",
            Url = string.Empty,
            IsCurrentPage = false
        },
        new BreadcrumbModel
        {
            Name = "Sports Mapping",
            Url = string.Empty,
            IsCurrentPage = true
        }
    };
    
    private PageHeaderModel _pageHeaderModel = new()
    {
        Title = "Sports Mapping 3",
        ShowLanguageDropDown = false
    };

    private bool _shouldRender;
    private MappingTab _activeTab = MappingTab.Unmapped;

    private enum MappingTab
    {
        Unmapped,
        Mapped
    }

    protected override bool ShouldRender() => _shouldRender;

    private void HandleTabClick(MappingTab tab)
    {
        _activeTab = tab;
        _shouldRender = true;
    }
}