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
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        public List<ConfigClientModel> Leagues { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            var configTemplateModel = ModalParameters.TryGet<ConfigTemplateModel>("ConfigTemplateModel");
            Leagues = new List<ConfigClientModel>();

            var assignedLeagues = await BackOfficeService.GetConfigClient(ConfigState.Assigned, new ConfigTemplateLeagueRequest(), (int)configTemplateModel.Id);
            if (assignedLeagues.StatusCode == StatusCode.Success)
            {
                Leagues = assignedLeagues.Result?.ClientConfigs ?? new List<ConfigClientModel>();
            }
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
