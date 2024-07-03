using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.SBC.ParlayBonus;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Pages.SBC
{
    public partial class ParlayBonus : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }
        [CascadingParameter(Name = "UserClientCodes")] List<string>? UserClientCodes { get; set; }

        public MetaData MetaData { get; set; }
        public ClientParlayBonusModel ClientParlayBonusModel { get; set; }
        public List<ParlayBonusConfig> TempConfig { get; set; }
        public List<SelectListItemModel<Client>> Clients { get; set; }

        public long ClientId { get; set; }
        public bool IsEdit { get; set; }
        public bool IsPreloading { get; set; }
        public bool IsEmpty { get; set; }
        public string ToastMessage { get; set; }
        public bool IsAdding { get; set; }
        public int Skip { get; set; }
        public int TotalCount { get; set; }

        #endregion

        #region Fields
        private PageHeaderModel _pageHeaderModel;
        //public const string ToastTitle = "Parlay Bonus";
        public static Func<decimal, string, Task> OnDisableSaveActionAsync;
        private bool _isSaving;
        #endregion

        #region Methods     

        protected override async Task OnInitializedAsync()
        {
            _pageHeaderModel = new()
            {
                Title = "Parlay Bonus",
                IconClass = "ico ico-parlay-bonus"
            };
            ClientParlayBonusModel = new ClientParlayBonusModel() { ParlayBonusMinOdds = 1.20m };
            Clients = new List<SelectListItemModel<Client>>();

            TempConfig = new List<ParlayBonusConfig>();

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 10000,
                TotalPages = 100,
            };

            OnDisableSaveActionAsync = DisableSaveButton;

            await AuthenticateAccess.InvokeAsync();

            await LoadClients();
        }

        public async Task OnClientChangedEvent(long clientId)
        {
            ClientId = clientId;

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10
            };

            await LoadClientParlayBonus(clientId);
        }

        public async Task OnDeleteConfig(Models.ParlayBonusConfig parlayBonusConfig)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("ParlayBonusConfig", parlayBonusConfig);

            var ClientName = Clients.FirstOrDefault(x => x.Value.ClientId == ClientId).Name;

            parameters.Add("ClientName", ClientName);

            var options = new ModalOptions();
            ModalService.Open<DeleteModal>("Delete Parlay Bonus", parameters, options);
        }

        public async Task OnViewConfigLog(Models.ParlayBonusConfig parlayBonusConfig)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("ParlayBonusConfig", parlayBonusConfig);
            parameters.Add("ModalIcon", "<i class=\"fa fa-list-alt m-2\"></i>");

            var options = new ModalOptions();
            ModalService.Open<ViewLogModal>("Logs", parameters, options);
        }

        public async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;

            IsAdding = false;
            Skip = (MetaData.CurrentPage - 1) * MetaData.PageSize;
        }

        public async Task OnUpdateClickEvent()
        {
            IsEdit = !IsEdit;
        }

        public async Task OnAddClickEvent()
        {
            _isSaving = true;
            IsAdding = true;
            Models.ParlayBonusConfig newConfig = null;

            IsEdit = true;

            if (ClientParlayBonusModel.ParlayBonusConfig.Count == 0)
            {
                newConfig = new Models.ParlayBonusConfig()
                {
                    SelectionCount = 3,
                    ParlayBonus = 0m
                };
            }
            else
            {
                newConfig = new Models.ParlayBonusConfig()
                {
                    SelectionCount = ClientParlayBonusModel.ParlayBonusConfig.Last().SelectionCount + 1,
                    ParlayBonus = 0m
                };
            }

            TempConfig.Add(newConfig);
            ClientParlayBonusModel.ParlayBonusConfig.Add(newConfig);

            MetaData.TotalCount = ClientParlayBonusModel.ParlayBonusConfig.Count;
            MetaData.TotalPages = ((ClientParlayBonusModel.ParlayBonusConfig.Count - 1) / MetaData.PageSize) + 1;

            TotalCount = ClientParlayBonusModel.ParlayBonusConfig.Count;

            await SelectPage(MetaData.TotalPages);
            await InvokeAsync(StateHasChanged);
            _isSaving = false;
        }

        public async Task OnSaveClickEvent()
        {
            _isSaving = true;
            IsPreloading = true;

            var result = (await BackofficeService.AddOrUpdateParlayBonus(new ParlayBonusRequest()
            {
                ClientIdRef = ClientId,
                ParlayBonusConfig = ClientParlayBonusModel.ParlayBonusConfig.Select(s => new ParlayBonusConfigRequest()
                {
                    SelectionCount = s.SelectionCount,
                    ParlayBonus = (s.ParlayBonus / 100)
                }).ToList(),
                ParlayBonusMinOdds = ClientParlayBonusModel.ParlayBonusMinOdds
            }));

            if (result.StatusCode == Backoffice.Shared.Enums.StatusCode.Success)
            {
                ToastMessage = TranslationState.GetValue("Parlay bonus updated!");
            }
            else
            {
                ToastMessage = result.Message;
            }

            IsEdit = false;

            IsAdding = false;

            Skip = 0;

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10
            };

            await LoadClientParlayBonus(ClientId);
            await ShowToast();

            _isSaving = false;
        }

        public async Task OnCancelClickEvent()
        {
            ClientParlayBonusModel.ParlayBonusConfig.RemoveAll(c => TempConfig.Contains(c));

            TempConfig = new List<Models.ParlayBonusConfig>();

            IsEdit = false;

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

        public async Task OnDeleteConfigClickEvent(object config)
        {
            var configCasted = (Models.ParlayBonusConfig)config;

            ClientParlayBonusModel.ParlayBonusConfig.Remove(configCasted);

            MetaData.TotalCount = ClientParlayBonusModel.ParlayBonusConfig.Count;
            MetaData.TotalPages = ((ClientParlayBonusModel.ParlayBonusConfig.Count - 1) / MetaData.PageSize) + 1;

            if (MetaData.CurrentPage >= MetaData.TotalPages)
            {
                await SelectPage(MetaData.CurrentPage - 1);
            }

            await InvokeAsync(StateHasChanged);
        }

        [JSInvokable]
        public static async Task DisableSaveButtonOnInput(string inputValue, string id)
        {
            decimal.TryParse(inputValue, out var value);

            await OnDisableSaveActionAsync.Invoke(value, id);
        }

        public void Dispose()
        {
            ClientParlayBonusModel = null;
            TempConfig = null;
            Clients = null;

            BackofficeService.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        private async Task ShowToast()
        {
            await JSRuntime.InvokeVoidAsync("showToast");
        }

        private async Task LoadClients()
        {
            var clients = (await BackofficeService.GetClients()).Result;
            if (clients != null)
            {
                if (UserClientCodes != null && UserClientCodes.Any())
                {
                    clients.Clients = clients.Clients.Where(c => UserClientCodes.Contains(c.ClientCode.ToUpper())).ToList();
                }

                Clients = clients.Clients.OrderBy(x => x.ClientName).Select(s => new SelectListItemModel<Client>
                {
                    Value = s,
                    Name = s.ClientName
                }).ToList();

            }

            if (!Clients.Any())
                Clients.Add(new SelectListItemModel<Client>
                {
                    Value = new Client(),
                    Name = "No Clients Available"
                });
            else
                Clients.Insert(0, new SelectListItemModel<Client> { Value = new Client(), Name = "Choose client..." });

            //var selectedClients = Clients.Where(c => !string.IsNullOrEmpty(c.Value.ClientCode)).Select(s => s.Value).ToList();            
        }

        private async Task LoadClientParlayBonus(long clientId, bool isSkip = true)
        {
            IsPreloading = true;
            IsEmpty = true;

            var result = (await BackofficeService.GetClientParlayBonus(new GetParlayBonusRequest()
            {
                ClientIdRef = clientId,
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

                ClientParlayBonusModel.ParlayBonusConfig.RemoveAll(x => TempConfig.Contains(x));

                TempConfig = new List<Models.ParlayBonusConfig>();

                if (!IsAdding)
                {
                    IsEdit = false;
                }

                ClientParlayBonusModel = result;
                IsPreloading = false;

                IsEmpty = ClientParlayBonusModel.ParlayBonusConfig.Count > 0 ? false : true;

                TotalCount = ClientParlayBonusModel.ParlayBonusConfig.Count;

                if (ClientParlayBonusModel.ParlayBonusMinOdds == 0)
                {
                    ClientParlayBonusModel.ParlayBonusMinOdds = 1.20m;
                }
            } 
        }

        private async Task DisableSaveButton(decimal inputValue, string id)
        {
            var selectionCount = int.Parse(id.Replace("parlay-bonus-input-", ""));

            if (ClientParlayBonusModel.ParlayBonusConfig.Where(c => c.SelectionCount != selectionCount).Any(c => c.ParlayBonus < 0.01m
            || c.ParlayBonus > 5000m) || inputValue < 0.01m || inputValue > 5000m)
            {
                await JSRuntime.InvokeVoidAsync("disableSaveButton", true);
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("disableSaveButton", false);
            }
        }

        #endregion
    }
}