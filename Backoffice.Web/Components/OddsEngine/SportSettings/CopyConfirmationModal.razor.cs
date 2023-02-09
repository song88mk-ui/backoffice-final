using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class CopyConfirmationModal : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        public string ToClientName { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ToClientName = ModalParameters.TryGet<string>("ToClientName");
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnCopyClickEvent()
        {
            var request = new ClientSportSettingRequest
            {
                ClientCode = ModalParameters.TryGet<string>("ToClientCode"),
                UserId = UserModel.Id,
                UserName = UserModel.UserName,
                TimeStamp = DateTime.UtcNow,
                SportsSortOrder = ModalParameters.TryGet<List<SportsSortOrder>>("Sports")
            };

            var response = (await TradingService.AddOrUpdateClientSportSettings(request)).Result;

            if (response.StatusCode == StatusCode.Success)
            {
                ModalService.Cancel();
                await Modal.OnSaveClickCallback.InvokeAsync();
            }
        }

        public void Dispose()
        {
        }

        #endregion
    }
}