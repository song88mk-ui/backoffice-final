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
    public partial class AddOrEditMarketGroupModal : IDisposable
    {
        #region Properties

        [Inject] IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }

        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        public ClientMarketTemplateGroupDTO MarketTemplateGroup { get; set; }
        public ClientMarketTemplateGroupDTO OldMarketTemplateGroup { get; set; }
        public MarketGroupModel MarketGroupModel { get; set; }

        public bool IsMarketGroupNameError { get; set; }
        public bool IsPreLoader { get; set; } = false;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            TradingService.Region = ModalParameters.TryGet<string>("Region").ToUpper();
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

        public async Task OnModalCancelEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnModalSaveEvent()
        {
            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);

            IsMarketGroupNameError = MarketTemplateGroup == null || string.IsNullOrEmpty(MarketTemplateGroup.Name) ? true : false;

            if (IsMarketGroupNameError)
                return;

            var isAdd = MarketTemplateGroup.MarketTemplateGroupId > 0 ? false : true;

            // create market group
            var oldClientMarketTemplateGroup = new List<ClientMarketTemplateGroupDTO>();
            oldClientMarketTemplateGroup.Add(OldMarketTemplateGroup);

            var clientMarketTemplateGroup = new List<ClientMarketTemplateGroupDTO>();
            clientMarketTemplateGroup.Add(new ClientMarketTemplateGroupDTO
            {
                MarketTemplateGroupId = MarketTemplateGroup.MarketTemplateGroupId,
                Name = MarketTemplateGroup.Name,
                SortOrder = MarketTemplateGroup.SortOrder,
                IsActive = MarketTemplateGroup.IsActive,
                MarketTemplateIds = MarketTemplateGroup.MarketTemplateIds,
                FixtureStatusId = MarketGroupModel.FixtureStatusId
            });

            var marketTemplateGroupResponse = await TradingService.AddOrUpdateClientMarketTemplateGroup(new ClientPostMarketTemplateGroupRequest
            {
                ClientId = MarketGroupModel.ClientId,
                SportId = MarketGroupModel.SportId,
                IsAdd = isAdd,
                MarketTemplateGroups = clientMarketTemplateGroup
            });

            if (marketTemplateGroupResponse.StatusCode == StatusCode.Success)
            {
                //activity log
                await BackOfficeService.AddActivityLog(new ActivityLogModel
                {
                    Page = SportsbookClientPermission.SBC_OE_MarketSetting_Group.ToString().Replace("_", "."),
                    Payload = $"{MarketGroupModel.ClientCode}.{MarketGroupModel.SportId}.{MarketGroupModel.FixtureStatusId}.{OldMarketTemplateGroup.MarketTemplateGroupId}",
                    Activity = isAdd ? Activity.Create.ToString() : Activity.Update.ToString(),
                    From = isAdd ? "-" : JsonSerializer.Serialize(oldClientMarketTemplateGroup),
                    To = JsonSerializer.Serialize(clientMarketTemplateGroup),
                    UserId = UserModel.Id,
                    UserName = UserModel.UserName
                });

                ModalService.Cancel();
                await Modal.OnSaveClickCallback.InvokeAsync(MarketTemplateGroup.Name);
            }
            else
            {
                IsMarketGroupNameError = true;
            }

            IsPreLoader = false;
            ModalService.Cancel();
        }

        public void Dispose()
        {
            
        }

        #endregion
    }
}
