using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.OddsEngine.ManageBoost;
using Backoffice.Web.Components.OddsEngine.MarketGroups;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static Backoffice.Shared.Models.Response.FastOddsSportsResponse;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MarketGroups : IDisposable
    {
        #region Properties
        [Inject] IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public bool IsPreLoader { get; set; }
        public bool IsSelectAll { get; set; }
        public string TableClass { get; set; }

        public MarketGroupModel MarketGroupModel { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            MarketGroupModel = new MarketGroupModel();
            TableClass = "table-edit-market-group";
            IsPreLoader = true;
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            //if (firstRender)
            //{
            //    // "tr > td.actions > a.drag-handle"
            //    await JSRuntime.InvokeVoidAsync("initSort", $".{TableClass}", ".drag-handle");
            //}
        }
        public async Task OnMarketGroupAdd()
        {
            var marketTemplateGroup = new ClientMarketTemplateGroupDTO
            {
                FixtureStatusId = MarketGroupModel.FixtureStatusId
            };

            var parameters = new ModalParameters();
            parameters.Add("MarketGroupModel", MarketGroupModel);
            parameters.Add("MarketTemplateGroup", marketTemplateGroup);
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<AddOrEditMarketGroupModal>($"Add Market Group", parameters, options);
        }
        public async Task OnUpdateMarketGroup()
        {
            MarketGroupModel.IsUpdate = true;

            await JSRuntime.InvokeVoidAsync("initSort", $".{TableClass}", ".drag-handle");
            await JSRuntime.InvokeVoidAsync("enableSort", $".{TableClass}");

            //await InvokeAsync(StateHasChanged);
        }
        public async Task OnEditMarketGroup(int marketTemplateGroupId)
        {
            var marketTemplateGroup = new ClientMarketTemplateGroupDTO
            {
                FixtureStatusId = MarketGroupModel.FixtureStatusId,
                MarketTemplateGroupId = marketTemplateGroupId
            };

            var parameters = new ModalParameters();

            parameters.Add("MarketGroupModel", MarketGroupModel);
            parameters.Add("MarketTemplateGroup", marketTemplateGroup);
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");
            parameters.Add("IsEdit", true);

            var options = new ModalOptions();
            ModalService.Open<AddOrEditMarketGroupModal>($"Edit Market Group", parameters, options);
        }

        public async Task OnDeleteMarketGroup(ClientMarketTemplateGroupDTO marketTemplateGroup)
        {
            var parameters = new ModalParameters();

            parameters.Add("MarketGroupModel", MarketGroupModel);
            parameters.Add("MarketTemplateGroup", marketTemplateGroup);
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<DeleteMarketGroupModal>($"Delete Market Group", parameters, options);
        }
        public async Task OnCanceUpdateClickedEvent()
        {
            MarketGroupModel.IsUpdate = false;

            await JSRuntime.InvokeVoidAsync("disableSort", $".{TableClass}");
        }

        public async Task OnCopyMarketGroup()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<CopyMarketGroupModal>($"Edit Market Group", parameters, options);
        }
        public async Task OnViewLogs()
        {
            var parameters = new ModalParameters();
            parameters.Add("MarketGroupModel", MarketGroupModel);
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<MarketGroupViewLogsModal>($"View Logs Market Group", parameters, options);
        }
        public async Task OnViewMarketGroup(string MarketTemplateGroupName, string marketTemplateIds)
        {
            var parameters = new ModalParameters();
            parameters.Add("ClientCode", MarketGroupModel.ClientCode);
            parameters.Add("SportId", MarketGroupModel.SportId);
            parameters.Add("FixtureStatusId", MarketGroupModel.FixtureStatusId);
            parameters.Add("MarketTemplateGroupName", MarketTemplateGroupName);
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<ViewMarketGroupModal>($"View Market Group", parameters, options);
        }
        protected async Task CheckboxSelectAllClicked(CheckBoxModel model)
        {
            IsSelectAll = model.Checked;
            if (MarketGroupModel != null && MarketGroupModel.MarketTemplateGroups.Any())
            {
                foreach (var marketTemplateGroup in MarketGroupModel.MarketTemplateGroups)
                {
                    marketTemplateGroup.IsActive = model.Checked;
                }
            }
        }
        protected async Task CheckboxMarketGroupClicked(CheckBoxModel model)
        {
            ((ClientMarketTemplateGroupDTO)model.Param1).IsActive = model.Checked;

            IsSelectAll = MarketGroupModel != null && !MarketGroupModel.MarketTemplateGroups.Any(s => s.IsActive == false);
        }
        public async Task OnSaveMarketGroupClickEvent(object item)
        {
            MarketGroupModel.IsUpdate = false;

            await OnInit();
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities
        public async Task OnInit()
        {
            PageTitle = "Market Groups";
            IconClass = string.Empty;
            IsPreLoader = true;
            MarketGroupModel.IsUpdate = false;

            await InvokeAsync(StateHasChanged);

            if (!string.IsNullOrEmpty(MarketGroupModel.ClientCode) && MarketGroupModel.SportId != 0)
            {
                var marketTemplateGroupResponse = (await TradingService.GetClientMarketTemplateGroups(MarketGroupModel.ClientCode, MarketGroupModel.SportId, MarketGroupModel.FixtureStatusId)).Result;

                MarketGroupModel.MarketTemplateGroups = marketTemplateGroupResponse.ClientMarketTemplateGroups
                    .Select(s => new ClientMarketTemplateGroupDTO
                    {
                        MarketTemplateGroupId = s.MarketTemplateGroupId,
                        Name = s.Name,
                        MarketTemplateIds = s.MarketTemplateIds,
                        SortOrder = s.SortOrder,
                        IsActive = s.IsActive,
                        FixtureStatusId = MarketGroupModel.FixtureStatusId
                    }).ToList();

                MarketGroupModel.OldMarketTemplateGroups = marketTemplateGroupResponse.ClientMarketTemplateGroups
                    .Select(s => new ClientMarketTemplateGroupDTO
                    {
                        MarketTemplateGroupId = s.MarketTemplateGroupId,
                        Name = s.Name,
                        MarketTemplateIds = s.MarketTemplateIds,
                        SortOrder = s.SortOrder,
                        IsActive = s.IsActive,
                        FixtureStatusId = MarketGroupModel.FixtureStatusId
                    }).ToList();
            }

            IsSelectAll = MarketGroupModel != null && !MarketGroupModel.MarketTemplateGroups.Any(s => s.IsActive == false);
            await SortOrderRow();
            IsPreLoader = false;
        }
        private async Task SortOrderRow()
        {
            if (MarketGroupModel != null && MarketGroupModel.MarketTemplateGroups.Any())
            {
                MarketGroupModel.MarketTemplateGroups = MarketGroupModel.MarketTemplateGroups.Select((s, index) =>
                {
                    s.SortOrder = index + 1;

                    return s;
                }).ToList();
            }

            await InvokeAsync(StateHasChanged);
        }
        #endregion
    }
}
