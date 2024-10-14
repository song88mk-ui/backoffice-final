using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class DeleteTemplateModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackofficeService BackofficeService { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        public ConfigTemplateModel ConfigTemplateModel { get; set; }

        #endregion
        private bool _isPreloader;
        private bool _isEmptyLeagues;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _isPreloader = false;
            ConfigTemplateModel = ModalParameters.TryGet<ConfigTemplateModel>("ConfigTemplateModel");
            _isEmptyLeagues = !(ConfigTemplateModel.ClientsCount > 0 || ConfigTemplateModel.LeaguesCount > 0);
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnApplyClickEvent()
        {
            _isPreloader = true;
            var response = await BackofficeService.DeleteConfigTemplate(ConfigTemplateModel.Id);

            if (response.StatusCode == StatusCode.Success)
            {
                await BackofficeService.AddActivityLog(new ActivityLogModel()
                {
                    Page = SportsbookClientPermission.SBC_OE_Config.ToString().Replace("_", "."),
                    Payload = $"{ConfigTemplateModel.SportId}.{ConfigTemplateModel.FixtureStatusId}.{ConfigTemplateModel.Name}.T".Replace(" ", "_"),
                    From = JsonSerializer.Serialize(ConfigTemplateModel),
                    To = "-",
                    Activity = Activity.Delete.ToString(),
                    UserId = UserModel.Id,
                    UserName = UserModel.UserName,
                    TimeStamp = DateTime.UtcNow
                });

                await Modal.OnSaveClickCallback.InvokeAsync($"{ConfigTemplateModel.Name} has successfully been removed.");
            }

            _isPreloader = false;
            ModalService.Cancel();
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
