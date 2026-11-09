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

        [Parameter] public string Region { get; set; } = "HK";

        public IJSStreamReference? DataReference { get; set; }

        public bool IsPreLoader { get; set; }
        public bool IsSelectAll { get; set; }
        public string TableClass { get; set; }

        public static MarketGroupModel MarketGroupModel { get; set; } = new MarketGroupModel();
        public List<BreadcrumbModel> Breadcrumbs { get; set; }

        #endregion

        #region Fields
        private PageHeaderModel _pageHeaderModel;
        private bool _isDelete = false;
        private bool _isUpdate = false;
        private int _tempUpdateGroupId = -1;
        private bool _pendingAdd = false;

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            _tradingService.Region = Region.ToUpper();
            _pageHeaderModel = new()
            {
                Title = "Market Groups",
                ShowLanguageDropDown = false
            };
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

            _modalService.OnClose += OnModalClosed;

            await PageState.AuthenticateAccessEvent.InvokeAsync();
        }

        private async void OnModalClosed(ModalResult result)
        {
            if (_pendingAdd)
            {
                _pendingAdd = false;
                await InvokeAsync(async () =>
                {
                    await OnInit();
                    StateHasChanged();
                });
            }
        }

        public async Task OnAddClickedEvent()
        {
            _pendingAdd = true;

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
            parameters.Add("Region", Region);

            var options = new ModalOptions();
            _modalService.Open<AddOrEditMarketGroupModal>($"Add Market Group", parameters, options);
        }

        public async Task OnUpdateClickedEvent()
        {
            MarketGroupModel.IsUpdate = true;

            await _jsRuntime.InvokeVoidAsync("initSort", $".{TableClass}", ".drag-handle");
            await _jsRuntime.InvokeVoidAsync("enableSort", $".{TableClass}");
        }

        public async Task OnEditClickedEvent(int marketTemplateGroupId)
        {
            _pendingAdd = false;
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
            parameters.Add("Region", Region);

            var options = new ModalOptions();
            _modalService.Open<AddOrEditMarketGroupModal>($"Edit Market Group", parameters, options);
        }

        public async Task OnDeleteClickedEvent(ClientMarketTemplateGroupDTO marketTemplateGroup)
        {
            _pendingAdd = false;
            var parameters = new ModalParameters();
            _isDelete = true;

            parameters.Add("MarketGroupModel", MarketGroupModel);
            parameters.Add("MarketTemplateGroup", marketTemplateGroup);
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");
            parameters.Add("Region", Region);

            var options = new ModalOptions();
            _modalService.Open<DeleteMarketGroupModal>($"Delete Market Group", parameters, options);
        }

        public async Task OnCancelUpdateClickedEvent()
        {
            MarketGroupModel.IsUpdate = false;

            IsPreLoader = true;
            StateHasChanged();

            await _jsRuntime.InvokeVoidAsync("disableSort", $".{TableClass}");

            MarketGroupModel.MarketTemplateGroups = new List<ClientMarketTemplateGroupDTO>();
            MarketGroupModel.MarketTemplateGroups.AddRange(MarketGroupModel.OldMarketTemplateGroups);

            IsPreLoader = false;
            StateHasChanged();
        }

        public async Task OnCopyClickedEvent()
        {
            // BO-158: the client, sport and scope on screen are the source; the dialog picks the target clients.
            // The saved groups are what gets copied, so unsaved edits on the page are not part of the copy.
            var parameters = new ModalParameters();
            parameters.Add("ClientId", MarketGroupModel.ClientId);
            parameters.Add("ClientCode", MarketGroupModel.ClientCode);
            parameters.Add("SportId", MarketGroupModel.SportId);
            parameters.Add("SportName", MarketGroupModel.SportName);
            parameters.Add("FixtureStatusId", MarketGroupModel.FixtureStatusId);
            parameters.Add("Groups", MarketGroupModel.OldMarketTemplateGroups ?? new List<ClientMarketTemplateGroupDTO>());
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");
            parameters.Add("Region", Region);

            var options = new ModalOptions();
            _modalService.Open<CopyMarketGroupModal>($"Copy Market Group", parameters, options);
        }

        public async Task OnViewLogsClickedEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("MarketGroupModel", MarketGroupModel);
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            //parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");
            parameters.Add("Region", Region);

            var options = new ModalOptions();
            _modalService.Open<MarketGroupViewLogsModal>($"View Logs", parameters, options);
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
            parameters.Add("Region", Region);

            var options = new ModalOptions();
            _modalService.Open<ViewMarketGroupModal>($"View Market Group", parameters, options);
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
                    if (group != null)
                    {
                        group.Name = item.ToString();
                        StateHasChanged();
                    }
                }
            }
            else
            {
                MarketGroupModel.IsUpdate = false;
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

                var hasChange = false;

                if (MarketGroupModel.OldMarketTemplateGroups.Count != marketTemplateGroups.Count)
                    hasChange = true;
                else
                {
                    foreach (var oldModel in MarketGroupModel.OldMarketTemplateGroups)
                    {
                        var newModel = marketTemplateGroups.FirstOrDefault(c => c.MarketTemplateGroupId == oldModel.MarketTemplateGroupId);

                        if (newModel == null)
                        {
                            hasChange = true;
                            break;
                        }

                        if (!newModel.Name.Equals(oldModel.Name, StringComparison.OrdinalIgnoreCase) ||
                            (!newModel.MarketTemplateIds?.Equals(oldModel.MarketTemplateIds, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            newModel.IsActive != oldModel.IsActive ||
                            newModel.SortOrder != oldModel.SortOrder)
                        {
                            hasChange = true;
                            break;
                        }
                    }
                }

                //if (oldMarketGroupSerialize == newMarketGroupSerialize)
                if (!hasChange)
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

                var response = await _backofficeService.AddOrUpdateClientMarketTemplateGroup(request);

                if (response.StatusCode == StatusCode.Success)
                {
                    //activity log
                    await _backofficeService.AddActivityLog(new ActivityLogModel
                    {
                        Page = SportsbookClientPermission.SBC_OE_MarketSetting_Group.ToString().Replace("_", "."),
                        Payload = $"{MarketGroupModel.ClientCode}.{MarketGroupModel.SportId}.{MarketGroupModel.FixtureStatusId}.0",
                        Activity = Activity.Update.ToString(),
                        From = JsonSerializer.Serialize(MarketGroupModel.OldMarketTemplateGroups),
                        To = JsonSerializer.Serialize(marketTemplateGroups),
                        UserId = _userState.Model.Id,
                        UserName = _userState.Model.Name
                    });

                    // re-assign the updated data on market sort order properties
                    MarketGroupModel.IsUpdate = false;

                    await OnInit();
                }
            }

            await OnInit();
        }

        public async ValueTask DisposeAsync()
        {
            _modalService.OnClose -= OnModalClosed;

            if (DataReference is not null)
                await DataReference.DisposeAsync();

            DataReference = null;
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private async Task OnInit()
        {
            IsPreLoader = true;

            MarketGroupModel.IsUpdate = false;

            if (!string.IsNullOrEmpty(MarketGroupModel.ClientCode) && MarketGroupModel.SportId != 0)
            {
                var clientId = _userState.Model.TradingClients.FirstOrDefault(c => c.ClientCode == MarketGroupModel.ClientCode).ClientId;

                var marketTemplateGroupResponse = (await _backofficeService.GetClientMarketTemplateGroups(clientId, MarketGroupModel.SportId, MarketGroupModel.FixtureStatusId)).Result;

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
            DataReference = await _jsRuntime.InvokeAsync<IJSStreamReference>("getSortOrder", $".{TableClass}");

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
