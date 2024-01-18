using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.OddsEngine.Configuration;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Threading;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class Configuration2 : IDisposable
    {
        #region Properties
        [Inject] private ITradingService TradingService { get; set; }
        [Inject] private IBackofficeService BackofficeService { get; set; }
        [Inject] private IModalService ModalService { get; set; }
        [Parameter] public string Region { get; set; } = "hk";

        [Parameter]
        [SupplyParameterFromQuery(Name = "M")]
        public string? QueryMessage { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [CascadingParameter(Name = "UserClientCodes")] public List<string> UserClientCodes { get; set; }
        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        public List<SelectListItemModel<string>> Sports { get; set; }
        public List<SelectListItemModel<string>> Scopes { get; set; }

        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public ConfigTemplateNavMenu SelectedConfigNavMenu { get; set; }

        public bool IsPreleader = true;
        #endregion
        private int? _myTemplatesCount;
        private int? _assignedLeaguesCount;
        private int? _unAssignedLeaguesCount;
        private AlertModel _alertModel { get; set; }


        #region Methods
        protected override async Task OnInitializedAsync()
        {
            SelectedConfigNavMenu = ConfigTemplateNavMenu.MyTemplates;

            IsPreleader = true;
            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = "hk/odds-engine/config-templates",
                    IsCurrentPage = true
                }
            };

            CalculateConfigCounts();
            await Init();
        }

        public async Task Init()
        {
            await AuthenticateAccess.InvokeAsync();

            Sports = new List<SelectListItemModel<string>>();
            Scopes = new List<SelectListItemModel<string>>();

            await Task.WhenAll(new[] {
                Task.Run(async () => { Clients = await LoadClients(); }),
                Task.Run(async () => { Sports = await  LoadSports(); }),
                Task.Run(async () => { Scopes = await LoadScopes();})
            });
            IsPreleader = false;
        }
        public async Task OnConfigNavMenuSelected(ConfigTemplateNavMenu configNavMenu)
        {
            SelectedConfigNavMenu = configNavMenu;
        }

        public async Task OnCopyClientSettings()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center");
            parameters.Add("ModalDialogClass", "modal-md");
            parameters.Add("Region", Region);
            parameters.Add("OnCopyClientSettingsCallback", EventCallback.Factory.Create(this, OnCopyClientSettingsAlert));

            var options = new ModalOptions();
            ModalService.Open<CopyClientSettingsModal>($"Copy Client Settings", parameters);
        }

        public async Task OnCopyClientSettingsAlert()
        {
            _alertModel = new AlertModel()
            {
                Status = AlertStatus.Success,
                Message = "Copy Client Settings Successfully.",
                Opacity = 1.0m
            };
            //Guid = Guid.NewGuid();
        }

        public async Task OnNavMenuCallback()
        {
            CalculateConfigCounts();
        }
        public async Task OnModalSavedEvent(object payload)
        {
            StateHasChanged();
        }
        public void Dispose()
        {

        }

        #endregion

        #region Utilities
        private async void CalculateConfigCounts()
        {
            _myTemplatesCount = null;
            _assignedLeaguesCount = null;
            _unAssignedLeaguesCount = null;

            await InvokeAsync(async () =>
            {
                var response = await BackofficeService.GetTemplateConfigCount();
                _myTemplatesCount = response.Result;
                if (response.StatusCode == StatusCode.Success)
                    _myTemplatesCount = response.Result;
                else
                    _myTemplatesCount = -1;
                StateHasChanged();
            });
            await InvokeAsync(async () =>
            {
                var response = await BackofficeService.GetTemplateConfigCount(ConfigState.Assigned.ToString());
                _assignedLeaguesCount = response.Result;
                if (response.StatusCode == StatusCode.Success)
                    _assignedLeaguesCount = response.Result;
                else
                    _assignedLeaguesCount = -1;
                StateHasChanged();
            });
            await InvokeAsync(async () =>
            {
                var response = await BackofficeService.GetTemplateConfigCount(ConfigState.UnAssigned.ToString());
                if (response.StatusCode == StatusCode.Success)
                    _unAssignedLeaguesCount = response.Result;
                else
                    _unAssignedLeaguesCount = -1;

                StateHasChanged();
            });
        }
        private async Task<List<SelectListItemModel<string>>> LoadSports()
        {
            var response = (await TradingService.GetSportLookup()).Result;
            var result = new List<SelectListItemModel<string>>();

            if (response == null)
                return new List<SelectListItemModel<string>>();

            result = response.SportLookup.Select(s => new SelectListItemModel<string>()
            {
                Value = s.SportId.ToString(),
                Name = s.SportName
            }).ToList();

            return result;
        }
        private async Task<List<SelectListItemWithCheckboxModel<string>>> LoadClients()
        {
            var response = (await TradingService.GetClientList()).Result;
            var result = new List<SelectListItemWithCheckboxModel<string>>();

            if (response != null && response.Any())
            {
                if (UserClientCodes.Any())
                    response = response.Where(c => UserClientCodes.Contains(c.ClientCode.ToUpper())).ToList();

                result = response.Select(e =>
                    new SelectListItemWithCheckboxModel<string>()
                    {
                        IsChecked = true,
                        Name = e.ClientName,
                        Value = e.ClientCode.ToString()
                    }).ToList();

                result.Insert(0, new SelectListItemWithCheckboxModel<string>
                {
                    Value = "All",
                    Name = "All Clients",
                    IsChecked = true
                });
            }

            return result;
        }
        private async Task<List<SelectListItemModel<string>>> LoadScopes()
        {
            return new List<SelectListItemModel<string>>()
            {
                new SelectListItemModel<string>() { Name = "Live", Value = ((int)Scope.Live).ToString() },
                new SelectListItemModel<string>() { Name = "Prematch", Value = ((int)Scope.Prematch).ToString() }
            };
        }
        #endregion
    }
}