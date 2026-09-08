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

        public MetaData MetaData { get; set; }
        public ClientParlayBonusModel ClientParlayBonusModel { get; set; } = new ClientParlayBonusModel() { ParlayBonusMinOdds = 1.20m };
        public List<ParlayBonusConfig> TempConfig { get; set; }

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
            PageState.Page = this;

            _pageHeaderModel = new()
            {
                Title = "Parlay Bonus",
                IconClass = "ico ico-parlay-bonus"
            };

            TempConfig = new List<ParlayBonusConfig>();

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 10000,
                TotalPages = 100,
            };

            OnDisableSaveActionAsync = DisableSaveButton;

            await PageState.AuthenticateAccessEvent.InvokeAsync();
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

        public async Task OnDeleteConfig(ParlayBonusConfig parlayBonusConfig)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("ParlayBonusConfig", parlayBonusConfig);

            var ClientName = _userState.Model.SportsbookClients.FirstOrDefault(c => c.ClientId == ClientId);

            parameters.Add("ClientName", ClientName?.ClientName ?? string.Empty);

            var options = new ModalOptions();
            _modalService.Open<DeleteModal>("Delete Parlay Bonus", parameters, options);
        }

        public async Task OnViewConfigLog(Models.ParlayBonusConfig parlayBonusConfig)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("ParlayBonusConfig", parlayBonusConfig);
            parameters.Add("ModalIcon", "<i class=\"fa fa-list-alt m-2\"></i>");

            var options = new ModalOptions();
            _modalService.Open<ViewLogModal>("Logs", parameters, options);
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

            var result = (await _backofficeService.AddOrUpdateParlayBonus(new ParlayBonusRequest()
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
                ToastMessage = _translationState.GetValue("Parlay bonus updated!");
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

            IsEdit = true;

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

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        private async Task ShowToast()
        {
            await _jsRuntime.InvokeVoidAsync("showToast");
        }

        private async Task LoadClientParlayBonus(long clientId, bool isSkip = true)
        {
            IsPreloading = true;
            IsEmpty = true;

            if (ClientParlayBonusModel == null)
                ClientParlayBonusModel = new ClientParlayBonusModel() { ParlayBonusMinOdds = 1.20m };

            ClientParlayBonusModel.ParlayBonusConfig = new List<ParlayBonusConfig>();

            if (clientId > 0)
            {
                var result = (await _backofficeService.GetClientParlayBonus(new GetParlayBonusRequest()
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

                    IsEmpty = ClientParlayBonusModel.ParlayBonusConfig.Count > 0 ? false : true;

                    TotalCount = ClientParlayBonusModel.ParlayBonusConfig.Count;

                    if (ClientParlayBonusModel.ParlayBonusMinOdds == 0)
                    {
                        ClientParlayBonusModel.ParlayBonusMinOdds = 1.20m;
                    }
                }
            }

            IsPreloading = false;
        }

        private async Task DisableSaveButton(decimal inputValue, string id)
        {
            var selectionCount = int.Parse(id.Replace("parlay-bonus-input-", ""));

            if (ClientParlayBonusModel.ParlayBonusConfig.Where(c => c.SelectionCount != selectionCount).Any(c => c.ParlayBonus < 0.01m
            || c.ParlayBonus > 5000m) || inputValue < 0.01m || inputValue > 5000m)
            {
                await _jsRuntime.InvokeVoidAsync("disableSaveButton", true);
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync("disableSaveButton", false);
            }
        }

        #endregion
    }
}