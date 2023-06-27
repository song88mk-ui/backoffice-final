using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ModifyLeagueTemplateModal : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [Parameter] public EventCallback<bool> SaveTemplateCallback { get; set; }
        [Parameter] public List<ConfigClientViewModel> Leageues { get; set; }
        [Parameter] public List<ConfigTemplateModel> Tempalates { get; set; }

        public int SelectedSportId { get; set; }
        public int SelectedScopeId { get; set; }
        public string TemplateName { get; set; }
        public bool IsConfigNameExisting { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
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
        }

        public void Dispose()
        {

        }
        #endregion

        #region Utilities

        private void LoadTemplates()
        {
        }

        #endregion
    }
}
