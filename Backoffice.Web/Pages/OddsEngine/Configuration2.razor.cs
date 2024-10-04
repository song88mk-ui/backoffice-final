using Backoffice.Shared.Enums;
using Backoffice.Web.Components.OddsEngine.Configuration;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Config;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RabbitMQ.Client.Framing.Impl;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class Configuration2 : IDisposable
    {
        #region Properties
        [Inject] private ITradingService TradingService { get; set; }
        [Inject] private IBackofficeService BackofficeService { get; set; }
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }

        [Parameter] public string Region { get; set; } = "hk";

        [Parameter]
        [SupplyParameterFromQuery(Name = "M")]
        public string? QueryMessage { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        public List<BreadcrumbModel> Breadcrumbs { get; set; }

        #endregion
        private ConfigContentFilterModel _contentFilterModel = new ConfigContentFilterModel();
        private ConfigTemplateNavMenu _selectedConfigNavMenu;
        private ConfigNavMenuParamModel _navMenuParamModel;
        private bool _isPreleader = true;
        private AlertModel _alertModel;

        private List<SelectListItemModel<string>> _scopes;

        private string _logTitle = "config";

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            await PageState.AuthenticateAccessEvent.InvokeAsync();
            _selectedConfigNavMenu = ConfigTemplateNavMenu.MyTemplates;
            _scopes = new List<SelectListItemModel<string>>()
            {
                new SelectListItemModel<string>() { Name = "Live", Value = ((int)Scope.Live).ToString() },
                new SelectListItemModel<string>() { Name = "Prematch", Value = ((int)Scope.Prematch).ToString() }
            };
            _navMenuParamModel = new ConfigNavMenuParamModel()
            {
                MyTemplatesCount = -1,
                AssignedLeaguesCount = -1,
                UnAssignedLeaguesCount = -1
            };

            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = "hk/odds-engine/config-templates",
                    IsCurrentPage = true
                }
            };

            Init();
        }

        public async void Init()
        {
            CalculateConfigCounts();

            _isPreleader = true;
            _contentFilterModel = new ConfigContentFilterModel();
            _contentFilterModel.Scopes = _scopes;

            await Task.WhenAll(new[] {
                Task.Run(async () => {_contentFilterModel.Clients = await LoadClients();}),
                Task.Run(async () => { _contentFilterModel.Sports = await  LoadSports();})
            });

            _isPreleader = false;
            await InvokeAsync(StateHasChanged);
        }
        public void OnConfigNavMenuSelected(ConfigTemplateNavMenu configNavMenu) => _selectedConfigNavMenu = configNavMenu;

        public void OnCopyClientSettings()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center");
            parameters.Add("ModalDialogClass", "modal-md");
            parameters.Add("Region", Region);
            parameters.Add("OnCopyClientSettingsCallback", EventCallback.Factory.Create(this, (AlertModel alert) => OnCopyClientSettingsAlert(alert)));

            var options = new ModalOptions();
            ModalService.Open<CopyClientSettingsModal>($"Copy Client Settings", parameters);
        }

        public void OnCopyClientSettingsAlert(AlertModel alert)
        {
            Init();

            _alertModel = alert;
        }

        public void OnNavMenuCallback() => CalculateConfigCounts();
        public void Dispose()
        {

        }

        #endregion

        #region Utilities
        private CancellationTokenSource _cancellationTokenSource;
        private async void CalculateConfigCounts()
        {
            _cancellationTokenSource?.Cancel();

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            await ExecuteWithCancellation(async () =>
            {
                var response = await BackofficeService.GetTemplateConfigCount(token: token);

                if (response.StatusCode == StatusCode.Success)
                    _navMenuParamModel.MyTemplatesCount = response.Result;

                StateHasChanged();
            }, token);

            await ExecuteWithCancellation(async () =>
            {
                var response = await BackofficeService.GetTemplateConfigCount(ConfigState.Assigned.ToString(), token);

                if (response.StatusCode == StatusCode.Success)
                    _navMenuParamModel.AssignedLeaguesCount = response.Result;

                StateHasChanged();
            }, token);

            await ExecuteWithCancellation(async () =>
            {
                var attempt = 0;
                while (attempt < 3)
                {
                    var response = await BackofficeService.GetTemplateConfigCount(ConfigState.UnAssigned.ToString(), token);

                    if (response.StatusCode == StatusCode.Success)
                    {
                        _navMenuParamModel.UnAssignedLeaguesCount = response.Result;
                        break;
                    }

                    attempt++;
                }

                StateHasChanged();
            }, token);

        }

        private async Task ExecuteWithCancellation(Func<Task> action, CancellationToken token)
        {
            try
            {
                await action();

                if (token.IsCancellationRequested)
                    return;
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
            }
        }
        private async Task<List<SelectListItemModel<string>>> LoadSports()
        {
            var response = await TradingService.GetSportLookup(TranslationState.ActiveLanguageCode.ToString());

            if (response.StatusCode != StatusCode.Success)
                return new List<SelectListItemModel<string>>();

            var sportLookup = response.Result;

            return sportLookup.SportLookup.Select(s => new SelectListItemModel<string>()
            {
                Value = s.SportId.ToString(),
                Name = s.SportName
            }).ToList();
        }
        private async Task<List<SelectListItemWithCheckboxModel<string>>> LoadClients()
        {
            var response = await TradingService.GetClientList();

            if (response.StatusCode != StatusCode.Success)
                return new List<SelectListItemWithCheckboxModel<string>>();

            var clientsLookup = response.Result;

            if (UserModel.ClientCodes.Any())
                clientsLookup = clientsLookup.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();

            if (!clientsLookup.Any())
                return new List<SelectListItemWithCheckboxModel<string>>();

            var result = clientsLookup.Select(e =>
                new SelectListItemWithCheckboxModel<string>()
                {
                    IsChecked = true,
                    Name = e.ClientName,
                    Value = e.ClientCode.ToString()
                }).ToList();

            if (clientsLookup.Count > 0)
                result.Insert(0, new SelectListItemWithCheckboxModel<string>
                {
                    Value = "All",
                    Name = "All Clients",
                    IsChecked = true
                });

            return result;
        }
        #endregion
    }
}