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

        #endregion
        private bool _isPreloader;
        private bool _isComplete = false;
        private List<ConfigClientModel> _leagues { get; set; }

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _isPreloader = true;
            var configTemplateId = ModalParameters.TryGet<long>("ConfigTemplateId");
            _leagues = new List<ConfigClientModel>();

            var assignedLeagues = await BackofficeService.GetConfigClient(ConfigState.Assigned, new ConfigTemplateLeagueRequest(), configTemplateId);
            if (assignedLeagues.StatusCode == StatusCode.Success)
            {
                _leagues = assignedLeagues.Result?.ClientConfigs ?? new List<ConfigClientModel>();
                _isComplete = assignedLeagues.Result?.IsComplete ?? false;
            }
            _isPreloader = false;
        }

        public void OnCloseClickEvent() => ModalService.Cancel();

        public void Dispose()
        {
            
        }
        #endregion
    }
}
