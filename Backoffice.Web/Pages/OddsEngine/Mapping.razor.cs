using Backoffice.Web.Models;
using System.Drawing;
using Microsoft.AspNetCore.Components;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.OddsEngine.Mapping;
using Backoffice.Web.Services;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class Mapping
    {
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        
        private List<BreadcrumbModel> _breadcrumbs;
        private PageHeaderModel _pageHeaderModel;

        private bool _shouldRender;

        private MappingTab _activeTab;

        private enum MappingTab
        {
            Unmapped,
            Mapped
        }

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            _pageHeaderModel = new()
            {
                Title = "Fixture Mapping",
                ShowLanguageDropDown = false
            };

            _breadcrumbs = new List<BreadcrumbModel>
            {
                new BreadcrumbModel 
                {
                    Name = "Mapping",
                    Url = string.Empty,
                    IsCurrentPage = false
                },
                new BreadcrumbModel 
                {
                    Name = "Fixture Mapping",
                    Url = string.Empty,
                    IsCurrentPage = true
                }
            };

            _activeTab = MappingTab.Unmapped;

            await PageState.AuthenticateAccessEvent.InvokeAsync();
        }

        protected override bool ShouldRender() => _shouldRender;

        private void HandleTabClick(MappingTab tab)
        {
            _activeTab = tab;
            _shouldRender = true;
        }

        public async Task OnEditMappingClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-lg");
            var options = new ModalOptions();

            ModalService.Open<EditMappingModal>("Edit Mapping Modal", parameters, options);
        }

        public async Task OnSaveClickEvent()
        {
        }
    }
}
