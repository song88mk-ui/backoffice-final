using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using ActivityLogModel = Backoffice.Shared.Models.ActivityLogModel;

namespace Backoffice.Web.Components.OddsEngine.MarketGroups
{
    public partial class DeleteMarketGroupModal : IDisposable
    {
        #region Properties

        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        public ClientMarketTemplateGroupDTO MarketTemplateGroup { get; set; }
        public ClientMarketTemplateGroupDTO OldMarketTemplateGroup { get; set; }
        public MarketGroupModel MarketGroupModel { get; set; }

        protected bool IsPreLoader { get; set; } = false;

        #endregion
        private AlertModel _alertModel;

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _tradingService.Region = ModalParameters.TryGet<string>("Region").ToUpper();
            IsPreLoader = false;
            MarketTemplateGroup = new ClientMarketTemplateGroupDTO();
            MarketGroupModel = ModalParameters.TryGet<MarketGroupModel>("MarketGroupModel");
            MarketTemplateGroup = ModalParameters.TryGet<ClientMarketTemplateGroupDTO>("MarketTemplateGroup");
            OldMarketTemplateGroup = new ClientMarketTemplateGroupDTO()
            {
                MarketTemplateGroupId = MarketTemplateGroup.MarketTemplateGroupId,
                Name = MarketTemplateGroup.Name,
                SortOrder = MarketTemplateGroup.SortOrder,
                IsActive = MarketTemplateGroup.IsActive,
                FixtureStatusId = MarketTemplateGroup.FixtureStatusId,
                MarketTemplateIds = MarketTemplateGroup.MarketTemplateIds
            };
        }

        public async Task OnCancelModalClick()
        {
            _modalService.Cancel();
        }

        public async Task OnDeleteModalClick()
        {
            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);

            if (MarketTemplateGroup.Name.ToLower() == "all markets")
            {
                _alertModel = new AlertModel()
                {
                    Status = AlertStatus.Error,
                    Message = "Group name \"All Markets\" cannot be deleted."
                };

                IsPreLoader = false;

                await InvokeAsync(StateHasChanged);
                return;
            }

            var marketTemplateGroupResponse = await _backofficeService.DeleteClientMarketTemplateGroup(new ClientDeleteMarketTemplateGroupRequest
            {
                ClientId = MarketGroupModel.ClientId,
                SportId = MarketGroupModel.SportId,
                FixtureStatusId = MarketGroupModel.FixtureStatusId,
                MarketTemplateGroup = MarketTemplateGroup
            });

            if (marketTemplateGroupResponse.StatusCode == StatusCode.Success)
            {
                _alertModel = new AlertModel()
                {
                    Status = AlertStatus.Success,
                    Message = $"Market group {MarketTemplateGroup.Name} is successfully deleted."
                };

                //activity log
                await _backofficeService.AddActivityLog(new ActivityLogModel
                {
                    Page = SportsbookClientPermission.SBC_OE_MarketSetting_Group.ToString().Replace("_", "."),
                    Payload = $"{MarketGroupModel.ClientCode}.{MarketGroupModel.SportId}.{MarketGroupModel.FixtureStatusId}",
                    Activity = Activity.Delete.ToString(),
                    From = JsonSerializer.Serialize(OldMarketTemplateGroup),
                    To = "-",
                    UserId = _userState.Model.Id,
                    UserName = _userState.Model.Name
                });

                _modalService.Cancel();
                await Modal.OnSaveClickCallback.InvokeAsync($"{MarketTemplateGroup.MarketTemplateGroupId}");
            }
            else
            {
                _alertModel = new AlertModel()
                {
                    Status = AlertStatus.Error,
                    Message = $"Error in deleting {MarketTemplateGroup.Name}."
                };
            }

            IsPreLoader = false;
            _modalService.Cancel();
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
