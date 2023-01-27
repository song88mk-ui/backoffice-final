using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.ParlayBonus;
using Backoffice.Web.Hubs;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Pages.SBC
{
    public partial class ParlayBonus : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [CascadingParameter(Name = "UserModel")] UserModel UserModel { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public MetaData MetaData { get; set; } = new MetaData();
        public ClientParlayBonusModel ClientParlayBonusResponse { get; set; }
        public long ClientId { get; set; }
        public bool IsUpdating { get; set; } = false;
        public List<ParlayBonusConfig> TempConfig { get; set; }
        public bool IsPreloading { get; set; }
        public List<SelectListItemModel<ClientModel.Client>> Clients { get; set; }
        public bool IsEmpty { get; set; }
        public const string ToastTitle = "Parlay Bonus";
        public string ToastMessage { get; set; }
        public bool IsAdding { get; set; }
        public int Skip { get; set; }
        public int TotalCount { get; set; }
        #endregion

        #region Methods                
        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Parlay Bonus";
            IconClass = "ico ico-parlay-bonus";
            ClientParlayBonusResponse = new ClientParlayBonusModel() { ParlayBonusMinOdds = 1.20m };
            Clients = new List<SelectListItemModel<ClientModel.Client>>();

            TempConfig = new List<ParlayBonusConfig>();

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 10000,
                TotalPages = 100,
            };

            await LoadClients();
        }

        public async Task LoadClients()
        {
            var clients = (await BackOfficeService.GetClients()).Result;
            if (clients != null)
            {

                if (UserModel.ClientCodes.Any())
                {
                    clients.Clients = clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();
                }

                Clients = clients.Clients.Select(s => new SelectListItemModel<ClientModel.Client>
                {
                    Value = s,
                    Name = s.ClientName
                }).ToList();

            }

            if (!Clients.Any())
                Clients.Add(new SelectListItemModel<ClientModel.Client>
                {
                    Value = new ClientModel.Client(),
                    Name = "No Clients Available"
                });
            else
                Clients.Insert(0, new SelectListItemModel<ClientModel.Client> { Value = new ClientModel.Client(), Name = "Choose client..." });

            //var selectedClients = Clients.Where(c => !string.IsNullOrEmpty(c.Value.ClientCode)).Select(s => s.Value).ToList();            
        }

        public async Task OnClientChangedEvent(long ClientId)
        {
            this.ClientId = ClientId;
            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10
            };
            await LoadClientParlayBonus(ClientId);
        }

        public async Task LoadClientParlayBonus(long ClientId, bool isSkip = true)
        {
            IsPreloading = true;
            IsEmpty = true;

            var result = (await BackOfficeService.GetClientParlayBonus(new GetParlayBonusRequest()
            {
                ClientIdRef = ClientId,
                Page = MetaData.CurrentPage,
                PageSize = MetaData.PageSize
            })).Result;

            if (result != null)
            {
                MetaData = new MetaData()
                {
                    CurrentPage = result.Page,
                    PageSize = result.PageSize,
                    TotalCount = result.Total,
                    TotalPages = result.TotalPage
                };

                foreach (var config in result.ParlayBonusConfig)
                {
                    config.ParlayBonus = Convert.ToDecimal((config.ParlayBonus * 100).ToString("#.00"));
                }

                ClientParlayBonusResponse.ParlayBonusConfig.RemoveAll(x => TempConfig.Contains(x));

                TempConfig = new List<ParlayBonusConfig>();

                if (!IsAdding)
                {
                    IsUpdating = false;
                }

                ClientParlayBonusResponse = result;
                IsPreloading = false;

                IsEmpty = ClientParlayBonusResponse.ParlayBonusConfig.Count > 0 ? false : true;

                TotalCount = ClientParlayBonusResponse.ParlayBonusConfig.Count;

                if (ClientParlayBonusResponse.ParlayBonusMinOdds == 0)
                {
                    ClientParlayBonusResponse.ParlayBonusMinOdds = 1.20m;
                }
            }
        }

        public async Task OnDeleteConfig(ParlayBonusConfig parlayBonusConfig)
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("ParlayBonusConfig", parlayBonusConfig);
            var ClientName = Clients.FirstOrDefault(x => x.Value.ClientId == ClientId).Name;
            parameters.Add("ClientName", ClientName);

            var options = new ModalOptions();
            ModalService.Open<DeleteModal>($"Delete Parlay Bonus", parameters, options);
        }

        public async Task OnViewConfigLog(ParlayBonusConfig parlayBonusConfig)
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("ParlayBonusConfig", parlayBonusConfig);
            parameters.Add("ModalIcon", "<i class=\"fa fa-list-alt m-2\"></i>");

            var options = new ModalOptions();
            ModalService.Open<ViewLogModal>($"Logs", parameters, options);
        }

        protected async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;

            IsAdding = false;
            Skip = (MetaData.CurrentPage - 1) * MetaData.PageSize;

            //await LoadClientParlayBonus(ClientId);
        }

        public async Task OnUpdateClick()
        {
            IsUpdating = !IsUpdating;
        }

        public async Task OnAddClick()
        {
            IsAdding = true;
            ParlayBonusConfig newConfig = null;

            IsUpdating = true;

            if (ClientParlayBonusResponse.ParlayBonusConfig.Count == 0)
            {
                newConfig = new ParlayBonusConfig()
                {
                    SelectionCount = 3,
                    ParlayBonus = 0m
                };
            }
            else
            {
                newConfig = new ParlayBonusConfig()
                {
                    SelectionCount = ClientParlayBonusResponse.ParlayBonusConfig.Last().SelectionCount + 1,
                    ParlayBonus = 0m
                };
            }

            TempConfig.Add(newConfig);
            ClientParlayBonusResponse.ParlayBonusConfig.Add(newConfig);

            MetaData.TotalCount = ClientParlayBonusResponse.ParlayBonusConfig.Count;
            MetaData.TotalPages = ((ClientParlayBonusResponse.ParlayBonusConfig.Count - 1) / MetaData.PageSize) + 1;

            TotalCount = ClientParlayBonusResponse.ParlayBonusConfig.Count;

            await SelectPage(MetaData.TotalPages);
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnSaveClick()
        {
            IsPreloading = true;

            var result = (await BackOfficeService.AddOrUpdateParlayBonus(new ParlayBonusRequest()
            {
                ClientIdRef = ClientId,
                ParlayBonusConfig = ClientParlayBonusResponse.ParlayBonusConfig.Select(x => new ParlayBonusConfigRequest()
                {
                    SelectionCount = x.SelectionCount,
                    ParlayBonus = (x.ParlayBonus / 100)
                }).ToList(),
                ParlayBonusMinOdds = ClientParlayBonusResponse.ParlayBonusMinOdds
            }));

            if (result.StatusCode == Backoffice.Shared.Enums.StatusCode.Success)
            {
                ToastMessage = "Parlay bonus updated!";
            }
            else
            {
                ToastMessage = result.Message;
            }

            IsUpdating = false;

            IsAdding = false;

            Skip = 0;

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10
            };

            await LoadClientParlayBonus(ClientId);
            await ShowToast();
        }

        public async Task OnCancelClick()
        {
            ClientParlayBonusResponse.ParlayBonusConfig.RemoveAll(x => TempConfig.Contains(x));

            TempConfig = new List<ParlayBonusConfig>();

            IsUpdating = false;

            IsAdding = false;

            Skip = 0;

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10
            };

            await LoadClientParlayBonus(ClientId);

            await InvokeAsync(StateHasChanged);
        }

        public async Task OnDeleteConfigClick(object config)
        {
            ParlayBonusConfig configCasted = (ParlayBonusConfig)config;

            ClientParlayBonusResponse.ParlayBonusConfig.Remove(configCasted);

            MetaData.TotalCount = ClientParlayBonusResponse.ParlayBonusConfig.Count;
            MetaData.TotalPages = ((ClientParlayBonusResponse.ParlayBonusConfig.Count - 1) / MetaData.PageSize) + 1;

            if (MetaData.CurrentPage >= MetaData.TotalPages) {
                await SelectPage(MetaData.CurrentPage - 1);
            }

            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
        }
        #endregion

        #region Utilities
        public async Task ShowToast()
        {
            await JSRuntime.InvokeVoidAsync("showToast");
        }
        #endregion
    }
}