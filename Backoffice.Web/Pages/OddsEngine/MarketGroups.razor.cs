using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.OddsEngine.ManageBoost;
using Backoffice.Web.Components.OddsEngine.MarketGroups;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MarketGroups : IDisposable
    {
        #region Properties
        [Inject] IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public bool IsPreLoader { get; set; }
        public bool IsSelectAll { get; set; }

        public MarketGroupModel MarketGroupModel { get; set; }
        #endregion

        #region Methods
        public async Task OnMarketGroupAdd()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<AddOrEditMarketGroupModal>($"Add Market Group", parameters, options);
        }

        public async Task OnEditMarketGroup()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");
            parameters.Add("IsEdit", true);

            var options = new ModalOptions();
            ModalService.Open<AddOrEditMarketGroupModal>($"Edit Market Group", parameters, options);
        }

        public async Task OnDeleteMarketGroup()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<DeleteMarketGroupModal>($"Delete Market Group", parameters, options);
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
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<MarketGroupViewLogsModal>($"View Logs Market Group", parameters, options);
        }

        public async Task OnViewMarketGroup()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<ViewMarketGroupModal>($"View Market Group", parameters, options);
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

            IsPreLoader = false;
        }
        #endregion
    }
}
