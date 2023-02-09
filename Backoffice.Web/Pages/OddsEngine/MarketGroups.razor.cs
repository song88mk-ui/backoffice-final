using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.OddsEngine.MarketGroups;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text;
using System.Text.Json;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MarketGroups : IAsyncDisposable, IDisposable
    {
        #region Properties

        [Inject] IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        public IJSStreamReference? DataReference { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public bool IsPreLoader { get; set; }
        public bool IsSelectAll { get; set; }
        public string TableClass { get; set; }

        public static MarketGroupModel MarketGroupModel { get; set; } = new MarketGroupModel();
        public List<BreadcrumbModel> Breadcrumbs { get; set; }

        #endregion

        #region Fields

        private bool _isDelete = false;
        private bool _isUpdate = false;
        private int _tempUpdateGroupId = -1;

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            TableClass = "table-edit-market-group";
            IsPreLoader = true;
            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Market Settings",
                    Url = string.Empty,
                    IsCurrentPage = false
                },
                new BreadcrumbModel
                {
                    Name = "Market Groups",
                    Url = string.Empty,
                    IsCurrentPage = true
                }
            };
        }

        public async Task OnAddClickedEvent()
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

        public async Task OnUpdateClickedEvent()
        {
            MarketGroupModel.IsUpdate = true;

            await JSRuntime.InvokeVoidAsync("initSort", $".{TableClass}", ".drag-handle");
            await JSRuntime.InvokeVoidAsync("enableSort", $".{TableClass}");
        }

        public async Task OnEditClickedEvent(int marketTemplateGroupId)
        {
            var marketTemplateGroup = MarketGroupModel.MarketTemplateGroups.Where(x => x.MarketTemplateGroupId == marketTemplateGroupId).FirstOrDefault();
            var parameters = new ModalParameters();
            _isUpdate = true;
            _tempUpdateGroupId = marketTemplateGroupId;

            parameters.Add("MarketGroupModel", MarketGroupModel);
            parameters.Add("MarketTemplateGroup", marketTemplateGroup);
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");
            parameters.Add("IsEdit", true);

            var options = new ModalOptions();
            ModalService.Open<AddOrEditMarketGroupModal>($"Edit Market Group", parameters, options);
        }

        public async Task OnDeleteClickedEvent(ClientMarketTemplateGroupDTO marketTemplateGroup)
        {
            var parameters = new ModalParameters();
            _isDelete = true;

            parameters.Add("MarketGroupModel", MarketGroupModel);
            parameters.Add("MarketTemplateGroup", marketTemplateGroup);
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<DeleteMarketGroupModal>($"Delete Market Group", parameters, options);
        }

        public async Task OnCancelUpdateClickedEvent()
        {
            MarketGroupModel.IsUpdate = false;

            IsPreLoader = true;
            StateHasChanged();

            await JSRuntime.InvokeVoidAsync("disableSort", $".{TableClass}");

            MarketGroupModel.MarketTemplateGroups = new List<ClientMarketTemplateGroupDTO>();
            MarketGroupModel.MarketTemplateGroups.AddRange(MarketGroupModel.OldMarketTemplateGroups);

            IsPreLoader = false;
            StateHasChanged();
        }

        public async Task OnCopyClickedEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ClientCode", MarketGroupModel.ClientCode);
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<CopyMarketGroupModal>($"Edit Market Group", parameters, options);
        }

        public async Task OnViewLogsClickedEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("MarketGroupModel", MarketGroupModel);
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<MarketGroupViewLogsModal>($"View Logs Market Group", parameters, options);
        }

        public async Task OnViewClickedEvent(string MarketTemplateGroupName, string marketTemplateIds)
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

        protected async Task CheckboxSelectAllClickedEvent(CheckBoxModel model)
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

        protected async Task CheckboxClickedEvent(CheckBoxModel model)
        {
            ((ClientMarketTemplateGroupDTO)model.Param1).IsActive = model.Checked;

            IsSelectAll = MarketGroupModel != null && !MarketGroupModel.MarketTemplateGroups.Any(s => s.IsActive == false);
        }

        public async Task OnSaveClickEvent(object item)
        {
            if (_isDelete)
            {
                if (item is not null)
                {
                    if (int.TryParse(item.ToString(), out int groupId))
                    {
                        MarketGroupModel.MarketTemplateGroups.Remove(MarketGroupModel.MarketTemplateGroups.Where(x => x.MarketTemplateGroupId == groupId).FirstOrDefault());
                        StateHasChanged();
                    }
                }
            }
            else if (_isUpdate)
            {
                if (item is not null)
                {
                    var group = MarketGroupModel.MarketTemplateGroups.Where(x => x.MarketTemplateGroupId == _tempUpdateGroupId).FirstOrDefault();
                    if(group != null)
                    {
                        group.Name = item.ToString();
                        StateHasChanged();
                    }
                }
            }
            else
            {
                MarketGroupModel.IsUpdate = false;

                await OnInit();
            }

            _isDelete = false;
            _isUpdate = false;
            _tempUpdateGroupId = -1;
        }

        public async Task OnSaveUpdateClickEvent(object item)
        {
            MarketGroupModel.IsUpdate = false;
            IsPreLoader = true;

            var marketGroups = await GetMarketGroupSortOrderUpdate();

            if (marketGroups.Any())
            {
                var marketTemplateGroups = new List<ClientMarketTemplateGroupDTO>();
                foreach (var marketTemplateGroup in MarketGroupModel.MarketTemplateGroups)
                {
                    marketTemplateGroup.SortOrder = marketGroups.FirstOrDefault(m => m.MarketTemplateGroupId == marketTemplateGroup.MarketTemplateGroupId).SortOrder;
                    marketTemplateGroups.Add(marketTemplateGroup);
                }

                var oldMarketGroupSerialize = JsonSerializer.Serialize(MarketGroupModel.OldMarketTemplateGroups);
                var newMarketGroupSerialize = JsonSerializer.Serialize(marketTemplateGroups);

                if (oldMarketGroupSerialize == newMarketGroupSerialize)
                {
                    await OnInit();
                    return;
                }

                var request = new ClientPostMarketTemplateGroupRequest
                {
                    ClientId = MarketGroupModel.ClientId,
                    SportId = MarketGroupModel.SportId,
                    IsAdd = false,
                    MarketTemplateGroups = marketTemplateGroups
                };

                var response = await TradingService.AddOrUpdateClientMarketTemplateGroup(request);

                if (response.StatusCode == StatusCode.Success)
                {
                    //activity log
                    await BackOfficeService.AddActivityLog(new ActivityLogModel
                    {
                        Page = SportsbookClientPermission.SBC_OE_MarketSetting_Group.ToString().Replace("_", "."),
                        Payload = $"{MarketGroupModel.ClientCode}.{MarketGroupModel.SportId}.{MarketGroupModel.FixtureStatusId}.0",
                        Activity = Activity.Update.ToString(),
                        From = JsonSerializer.Serialize(MarketGroupModel.OldMarketTemplateGroups),
                        To = JsonSerializer.Serialize(marketTemplateGroups),
                        UserId = UserModel.Id,
                        UserName = UserModel.UserName
                    });

                    // re-assign the updated data on market sort order properties
                    MarketGroupModel.IsUpdate = false;

                    await OnInit();
                }

                var AlertMessages = response.StatusCode == StatusCode.Success ? $"Market group order update for {MarketGroupModel.ClientCode} is successful" : $"Market sort order updated for {MarketGroupModel.ClientCode} failed";
            }

            await OnInit();
        }

        public async ValueTask DisposeAsync()
        {
            if (DataReference is not null)
                await DataReference.DisposeAsync();

            DataReference = null;
        }

        public void Dispose()
        {
            TradingService.Dispose();
            BackOfficeService.Dispose();
        }

        #endregion

        #region Utilities

        private async Task OnInit()
        {
            PageTitle = "Market Groups";
            IconClass = string.Empty;
            IsPreLoader = true;

            MarketGroupModel.IsUpdate = false;

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

            IsPreLoader = false;
        }

        private async Task<List<ClientMarketTemplateGroupDTO>> GetMarketGroupSortOrderUpdate()
        {
            DataReference = await JSRuntime.InvokeAsync<IJSStreamReference>("getSortOrder", $".{TableClass}");

            var selectionTemplate
                = new List<SortOrderModel>();

            using (var stream = await DataReference.OpenReadStreamAsync(maxAllowedSize: 10_000_000))
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    var hasLine = true;

                    while (hasLine)
                    {
                        var content = await reader.ReadLineAsync();

                        if (content == null)
                        {
                            hasLine = false;
                            continue;
                        }

                        selectionTemplate.Add(JsonSerializer.Deserialize<SortOrderModel>(content));
                    }
                }
            }

            var marketTemplateGroup = new List<ClientMarketTemplateGroupDTO>();

            foreach (var item in MarketGroupModel.MarketTemplateGroups)
            {
                var newSortOrder = selectionTemplate.Where(x => x.RowId == $"name-{item.MarketTemplateGroupId.ToString()}").FirstOrDefault();

                if (newSortOrder != null)
                {
                    marketTemplateGroup.Add(new ClientMarketTemplateGroupDTO()
                    {
                        MarketTemplateGroupId = item.MarketTemplateGroupId,
                        Name = item.Name,
                        SortOrder = newSortOrder.SortOrder,
                        MarketTemplateIds = item.MarketTemplateIds,
                        IsActive = item.IsActive,
                        FixtureStatusId = item.FixtureStatusId
                    });
                }
            }

            return marketTemplateGroup.OrderBy(x => x.SortOrder).ToList();
        }

        #endregion
    }
}
