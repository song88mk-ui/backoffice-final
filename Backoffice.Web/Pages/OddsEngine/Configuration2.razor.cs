using Azure.Storage.Blobs.Models;
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
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }

        [Parameter]
        [SupplyParameterFromQuery(Name = "M")]
        public string? QueryMessage { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }


        #endregion

        #region Fields

        private ConfigContentFilterModel _contentFilterModel = new ConfigContentFilterModel();
        private ConfigNavMenuParamModel _navMenuParamModel;
        private AlertModel _alertModel;
        private CancellationTokenSource? _cts;

        private List<BreadcrumbModel> _breadcrumbs;
        private List<SelectListItemModel<string>> _scopes;
        private ConfigState _currentState;
        private List<string> _userClientCodes = new();
        private string? _queryMessage;

        private bool _isPreloader = true;
        private bool _shouldRender = true;
        private LanguageCode _languageCode;
        private bool _init = true;

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            await PageState.AuthenticateAccessEvent.InvokeAsync();

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

            SetCurrentState(ConfigTemplateNavMenu.MyTemplates);

            _breadcrumbs = new List<BreadcrumbModel>()
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
        protected override async Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameters();

            if (_shouldRender)
            {
                if (_languageCode != _translationState.ActiveLanguageCode)
                {
                    _languageCode = _translationState.ActiveLanguageCode;
                    _contentFilterModel.Sports = await LoadSports();
                }

                _queryMessage = QueryMessage;
                _shouldRender = true;
            }

            _init = false;
        }

        protected override bool ShouldRender() => _shouldRender;
        protected override void OnAfterRender(bool firstRender) => _shouldRender = false;
        private bool ValidateParameters() => _init || _languageCode != _translationState.ActiveLanguageCode ||  _queryMessage != QueryMessage;

        public void HandleConfigNavMenuSelection(ConfigTemplateNavMenu configNavMenu)
        {
            SetCurrentState(configNavMenu);

            _shouldRender = true;
        }

        public void HandleCopyClientClick()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center");
            parameters.Add("ModalDialogClass", "modal-md");
            parameters.Add("OnCopyClientSettingsCallback", EventCallback.Factory.Create(this, (AlertModel alert) => HandleCopyClientSettingsAlert(alert)));

            var options = new ModalOptions();
            ModalService.Open<CopyClientSettingsModal>($"Copy Client Settings", parameters);
        }

        public void HandleCopyClientSettingsAlert(AlertModel alert)
        {
            _alertModel = alert;

            Init();
        }

        public void HandleNavMenuCallback() => CalculateConfigCounts();

        public void Dispose()
        {

        }

        #endregion

        #region Utilities

        private async void Init()
        {
            CalculateConfigCounts();

            _isPreloader = true;
            _shouldRender = true;
            _contentFilterModel = new ConfigContentFilterModel();
            _contentFilterModel.Scopes = _scopes;

            await Task.WhenAll(new[] {
                Task.Run(async () => { _contentFilterModel.Clients = await LoadClients(); _shouldRender = true; }),
                Task.Run(async () => { _contentFilterModel.Sports = await LoadSports(); _shouldRender = true;})
            });

            _isPreloader = false;
            _shouldRender = true;
            await InvokeAsync(StateHasChanged);
        }
        private async void CalculateConfigCounts()
        {
            _cts?.Cancel();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            await ExecuteWithCancellation(async () =>
            {
                var response = await _backofficeService.GetTemplateConfigCount(token: token);

                if (response.StatusCode == StatusCode.Success)
                    _navMenuParamModel.MyTemplatesCount = response.Result;

                var rand = new Random();

                _navMenuParamModel.MyTemplatesCount = rand.Next(1, 1000);

                StateHasChanged();
            }, token);

            await ExecuteWithCancellation(async () =>
            {
                var response = await _backofficeService.GetTemplateConfigCount(ConfigState.Assigned.ToString(), token);

                if (response.StatusCode == StatusCode.Success)
                    _navMenuParamModel.AssignedLeaguesCount = response.Result;

                //await Task.Delay(5000);

                //var rand = new Random();

                //_navMenuParamModel.AssignedLeaguesCount = rand.Next(1, 1000);

                StateHasChanged();
            }, token);

            await ExecuteWithCancellation(async () =>
            {
                var attempt = 0;
                while (attempt < 3)
                {
                    var response = await _backofficeService.GetTemplateConfigCount(ConfigState.UnAssigned.ToString(), token);

                    if (response.StatusCode == StatusCode.Success)
                    {
                        _navMenuParamModel.UnAssignedLeaguesCount = response.Result;
                        break;
                    }

                    //await Task.Delay(8000);

                    //var rand = new Random();

                    //_navMenuParamModel.UnAssignedLeaguesCount = rand.Next(1, 1000);
                    //break;

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
            var response = await _tradingService.GetSportLookup(_languageCode.ToString());

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
            var response = await _tradingService.GetClientList();

            if (response.StatusCode != StatusCode.Success)
                return new List<SelectListItemWithCheckboxModel<string>>();

            var clientsLookup = response.Result;

            if (_userClientCodes.Any())
                clientsLookup = clientsLookup.Where(c => _userClientCodes.Contains(c.ClientCode.ToUpper())).ToList();

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
        private void SetCurrentState(ConfigTemplateNavMenu menu)
        {
            _navMenuParamModel.SelectedNavMenu = menu;
            _currentState = menu == ConfigTemplateNavMenu.Assigned ? ConfigState.Assigned : ConfigState.UnAssigned;
        }
        #endregion
    }
}