using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.MarketGroups
{
    public partial class CopyConfirmationMarketGroupModalBody : IDisposable
    {
        #region Properties

        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        public MarketGroupModel MarketGroupModel { get; set; }

        protected bool IsPreLoader { get; set; } = false;
        public Guid CopyToClientId { get; set; }
        public string CopyToClientCode { get; set; }
        public string CopyToClientName { get; set; }
        public int CopyToSportId { get; set; }
        public string CopyToSportName { get; set; }
        public int CopyToFixtureStatusId { get; set; }
        public string CopyToFixtureStatusName { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            MarketGroupModel = ModalParameters.TryGet<MarketGroupModel>("MarketGroupModel");
            CopyToClientId = ModalParameters.TryGet<Guid>("CopyToClientId");
            CopyToClientCode = ModalParameters.TryGet<string>("CopyToClientCode");
            CopyToClientName = ModalParameters.TryGet<string>("CopyToClientName");
            CopyToSportId = ModalParameters.TryGet<int>("CopyToSportId");
            CopyToSportName = ModalParameters.TryGet<string>("CopyToSportName");
            CopyToFixtureStatusId = ModalParameters.TryGet<int>("CopyToFixtureStatusId");
            CopyToFixtureStatusName = ModalParameters.TryGet<string>("CopyToFixtureStatusName");

            IsPreLoader = false;
        }

        public async Task OnCancelClickEvent()
        {
            _modalService.Cancel();
        }

        protected async Task OnCopyMarketGroup()
        {
            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);

            var request = new ClientCopyMarketTemplateGroupRequest
            {
                ClientId = MarketGroupModel.ClientId,
                SportId = MarketGroupModel.SportId,
                FixtureStatusId = MarketGroupModel.FixtureStatusId,
                CopyToClientId = CopyToClientId,
                CopyToSportId = CopyToSportId,
                CopyToFixtureStatusId = CopyToFixtureStatusId
            };

            var response = (await _tradingService.CopyClientMarketTemplateGroup(request));

            if (response.StatusCode == StatusCode.Success)
            {
                var fromData = new List<MarketGroupActivtyModel>();
                foreach (var item in MarketGroupModel.OldMarketTemplateGroups)
                {
                    fromData.Add(new MarketGroupActivtyModel()
                    {
                        MarketTemplateGroupId = item.MarketTemplateGroupId,
                        Name = item.Name,
                        SortOrder = item.SortOrder,
                        MarketTemplateIds = item.MarketTemplateIds,
                        IsActive = item.IsActive
                    });
                }
                var toData = new List<MarketGroupActivtyModel>();
                foreach (var item in MarketGroupModel.MarketTemplateGroups)
                {
                    toData.Add(new MarketGroupActivtyModel()
                    {
                        MarketTemplateGroupId = item.MarketTemplateGroupId,
                        Name = item.Name,
                        SortOrder = item.SortOrder,
                        MarketTemplateIds = item.MarketTemplateIds,
                        IsActive = item.IsActive
                    });
                }

                //activity log
                await _backofficeService.AddActivityLog(new ActivityLogModel
                {
                    Page = SportsbookClientPermission.SBC_OE_MarketSetting_Group.ToString().Replace("_", "."),
                    Payload = $"{CopyToClientCode}.{CopyToSportId}.{CopyToFixtureStatusId}.0",
                    Activity = Activity.Copy.ToString(),
                    From = JsonSerializer.Serialize(fromData),
                    To = JsonSerializer.Serialize(toData),
                    UserId = _userState.Model.Id,
                    UserName = _userState.Model.Name
                });

                await InvokeAsync(StateHasChanged);
            }

            IsPreLoader = false;
            _modalService.Cancel();
            await Modal.OnSaveClickCallback.InvokeAsync(true);
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
