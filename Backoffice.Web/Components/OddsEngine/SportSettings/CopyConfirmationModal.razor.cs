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
        [CascadingParameter] public CustomModal Modal { get; set; }

        public string ToClientName { get; set; }

        private bool IsPreloader { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ToClientName = ModalParameters.TryGet<string>("ToClientName");
            TradingService.Region = ModalParameters.TryGet<string>("Region").ToUpper();
            IsPreloader = false;
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnCopyClickEvent()
        {
            IsPreloader = true;
            await InvokeAsync(StateHasChanged);

            var request = new ClientSportSettingRequest
            {
                ClientCode = ModalParameters.TryGet<string>("ToClientCode"),
                UserId = _userState.Id,
                UserName = _userState.Name,
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