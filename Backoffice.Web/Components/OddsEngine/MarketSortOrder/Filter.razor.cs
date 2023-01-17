using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.MarketSortOrder
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public ITradingService TradingService { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        [Parameter] public EventCallback OnUpdateCallback { get; set; }
        [Parameter] public EventCallback OnCancelCallback { get; set; }
        [Parameter] public EventCallback OnSaveCallback { get; set; }
        [Parameter] public EventCallback<MarketSortOrderModel> MarketSortOrderModelCallback { get; set; }
        [Parameter] public EventCallback<string> OnMarketGroupCallback { get; set; }

        public List<SelectListItemModel<string>> Clients { get; set; }
        public string SelectedClientCode { get; set; }
        public List<SelectListItemModel<int>> Sports { get; set; }
        public int SelectedSportId { get; set; }
        public List<SelectListItemModel<int>> FixtureStatuses { get; set; }
        public int SelectedFixtureStatusId { get; set; }
        public List<MarketSortOrderModel.ClientMarketTemplateGroup> ClientMarketTemplateGroups { get; set; }
        public int SelectedMarketTemplateGroupId { get; set; }

        public bool IsUpdate { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Clients = new List<SelectListItemModel<string>>();
            Sports = new List<SelectListItemModel<int>>();
            FixtureStatuses = new List<SelectListItemModel<int>>();
            ClientMarketTemplateGroups = new List<MarketSortOrderModel.ClientMarketTemplateGroup>();

            IsUpdate = false;

            if (UserModel.ClientCodes != null)
            {
                var clients = (await TradingService.GetClientList()).Result;
                var roleName = UserModel.RoleName;
                
                if (roleName != "Super Admin")
                {
                    var clientCodes = UserModel.ClientCodes;
                    clients = clients.Where(c => clientCodes.Contains(c.ClientCode)).ToList();
                }

                Clients = clients.Select(s => new SelectListItemModel<string>
                {
                    Name = s.ClientName,
                    Value = s.ClientCode
                }).ToList();
            }
            else
            {
                Clients.Add(new SelectListItemModel<string>
                {
                    Value = "",
                    Name = "No Clients Available"
                });
            }

            SelectedClientCode = Clients.FirstOrDefault().Value;

            var sports = (await TradingService.GetSportLookup()).Result;

            if (sports != null)
            {
                Sports = sports.SportLookup.Select(s => new SelectListItemModel<int>
                {
                    Value = s.SportId,
                    Name = s.SportName
                }).ToList();
            }
            else
            {
                Sports.Add(new SelectListItemModel<int>
                {
                    Value = 0,
                    Name = "No Sports Available"
                });
            }

            SelectedSportId = Sports.FirstOrDefault().Value;

            FixtureStatuses.Add(new SelectListItemModel<int>
            {
                Value = (int)FixtureStatus.Prematch,
                Name = FixtureStatus.Prematch.ToString()
            });

            FixtureStatuses.Add(new SelectListItemModel<int>
            {
                Value = (int)FixtureStatus.Live,
                Name = FixtureStatus.Live.ToString()
            });

            SelectedFixtureStatusId = FixtureStatuses.FirstOrDefault().Value;

            await PrepareMarketSortOrderModel();
        }

        public async void OnClientChangedEvent(ChangeEventArgs e)
        {
            SelectedClientCode = e.Value.ToString();
            IsUpdate = false;

            await OnCancelCallback.InvokeAsync();
            await PrepareMarketSortOrderModel();
        }

        public async void OnSportChangedEvent(ChangeEventArgs e)
        {
            SelectedSportId = int.Parse(e.Value.ToString());
            IsUpdate = false;

            await OnCancelCallback.InvokeAsync();
            await PrepareMarketSortOrderModel();
        }

        public async void OnFixtureStatusChangedEvent(ChangeEventArgs e)
        {
            SelectedFixtureStatusId = int.Parse(e.Value.ToString());
            IsUpdate = false;

            await OnCancelCallback.InvokeAsync();
            await PrepareMarketSortOrderModel();
        }

        public async Task OnUpdateClickEvent()
        {
            IsUpdate = true;

            await OnUpdateCallback.InvokeAsync();
        }

        public async Task OnSaveClickEvent()
        {
            IsUpdate = false;

            await OnSaveCallback.InvokeAsync();
        }

        public async Task OnCancelClickEvent()
        {
            IsUpdate = false;

            await OnCancelCallback.InvokeAsync();
        }

        public async Task OnMarketGroupClickEvent(int marketTemplateGroupId)
        {
            SelectedMarketTemplateGroupId = marketTemplateGroupId;

            var marketTemplateGroup = ClientMarketTemplateGroups.FirstOrDefault(c => c.MarketTemplateGroupId == SelectedMarketTemplateGroupId);

            await OnMarketGroupCallback.InvokeAsync(marketTemplateGroup.Name);
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private async Task PrepareMarketSortOrderModel()
        {
            if (SelectedClientCode != "NA" && SelectedSportId != 0)
            {
                var marketTemplateResponse = (await TradingService.GetClientMarketTemplates(SelectedClientCode, SelectedSportId, SelectedFixtureStatusId)).Result;
                var marketTemplateGroupResponse = (await TradingService.GetClientMarketTemplateGroups(SelectedClientCode, SelectedSportId, SelectedFixtureStatusId)).Result;

                ClientMarketTemplateGroups = marketTemplateGroupResponse.ClientMarketTemplateGroups
                    .Select(s => new MarketSortOrderModel.ClientMarketTemplateGroup
                    {
                        MarketTemplateGroupId = s.MarketTemplateGroupId,
                        Name = s.Name,
                        MarketTemplateIds = s.MarketTemplateIds.Split(",").ToList(),
                        SortOrder = s.SortOrder,
                        IsSelected = s.Name == "All Markets" ? true : false,
                    }).ToList();

                SelectedMarketTemplateGroupId = ClientMarketTemplateGroups.FirstOrDefault().MarketTemplateGroupId;

                await MarketSortOrderModelCallback.InvokeAsync(new MarketSortOrderModel
                {
                    ClientCode = SelectedClientCode,
                    ClientId = marketTemplateResponse.ClientId,
                    SportId = SelectedSportId,
                    FixtureStatusId = SelectedFixtureStatusId,
                    MarketTemplates = marketTemplateResponse.MarketTemplates
                    .OrderBy(o => o.SortOrder)
                    .Select((s, index) => new MarketSortOrderModel.ClientMarketTemplate
                    {
                        MarketTemplateGroupNames = s.MarketTemplateGroupNames,
                        MarketTemplateId = s.MarketTemplateId,
                        MarketTemplateName = s.MarketTemplateName,
                        SortOrder = s.SortOrder > 1000 ? index + 1 : s.SortOrder
                    }).ToList(),
                    MarketTemplateGroups = ClientMarketTemplateGroups
                });
            }
        }

        #endregion
    }
}
