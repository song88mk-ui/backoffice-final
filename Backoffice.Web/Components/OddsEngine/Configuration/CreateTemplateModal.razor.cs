using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class CreateTemplateModal : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }
        [Inject] private IBackOfficeService BackOfficeService { get; set; }

        [Parameter] public EventCallback<bool> CreateTemplateCallback { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        public List<SelectListItemModel<int>> Sports { get; set; }

        public int SelectedSportId { get; set; }
        public int SelectedScopeId { get; set; }
        public string TemplateName { get; set; }
        public bool IsConfigNameExisting { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Sports = new List<SelectListItemModel<int>>();

            Sports = await LoadSports();
        }

        public async Task OnSportSelectChanged(ChangeEventArgs e)
        {
            SelectedSportId = int.Parse(e.Value.ToString());
        }

        public async Task OnScopeSelectChanged(ChangeEventArgs e)
        {
            SelectedScopeId = int.Parse(e.Value.ToString());
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnSaveClickEvent()
        {
            IsConfigNameExisting = false;

            var response = await BackOfficeService.AddOrUpdateConfigTemplate(new ConfigTemplateModel()
            {
                SportId = SelectedSportId,
                FixtureTypeId = SelectedScopeId,
                Name = TemplateName,                
            });

            if (response.StatusCode == StatusCode.Success)
            {                
                await CreateTemplateCallback.InvokeAsync(true);
                ModalService.Cancel();
            }
            else
            {
                IsConfigNameExisting = true;
            }            
        }

        public void Dispose()
        {
            
        }
        #endregion

        #region Utilities

        private async Task<List<SelectListItemModel<int>>> LoadSports()
        {
            var result = (await TradingService.GetSportLookup()).Result;

            if (result == null)
                return new List<SelectListItemModel<int>>();

            var sports = result.SportLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SportId,
                Name = s.SportName
            }).ToList();

            return sports;
        }

        #endregion
    }
}
