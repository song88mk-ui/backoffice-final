using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ViewAssignedModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackofficeService BackofficeService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        public List<ConfigClientModel> Leagues { get; set; }
        #endregion
        private bool _isPreloader;
        private bool _isComplete = false;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _isPreloader = true;
            var configTemplateModel = ModalParameters.TryGet<ConfigTemplateModel>("ConfigTemplateModel");
            Leagues = new List<ConfigClientModel>();

            var assignedLeagues = await BackofficeService.GetConfigClient(ConfigState.Assigned, new ConfigTemplateLeagueRequest(), (int)configTemplateModel.Id);
            if (assignedLeagues.StatusCode == StatusCode.Success)
            {
                Leagues = assignedLeagues.Result?.ClientConfigs ?? new List<ConfigClientModel>();
                _isComplete = assignedLeagues.Result?.IsComplete ?? false;
            }
            _isPreloader = false;
        }

        public async Task OnCloseClickEvent()
        {
            ModalService.Cancel();
        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
