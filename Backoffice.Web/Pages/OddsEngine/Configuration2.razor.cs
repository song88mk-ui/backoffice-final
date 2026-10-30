using Backoffice.Shared.Enums;
using Backoffice.Web.Components.OddsEngine.Configuration;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Config;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RabbitMQ.Client.Framing.Impl;
using Serilog;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class Configuration2 : IDisposable
    {
        #region Properties

        [Parameter]
        [SupplyParameterFromQuery(Name = "M")]
        public string? QueryMessage { get; set; }


        #endregion

        #region Fields

        private ConfigContentFilterModel _contentFilterModel = new ConfigContentFilterModel();
        private ConfigNavMenuParamModel _navMenuParamModel;
        private AlertModel _alertModel;
        private bool _loadFailureShown;
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
        }
        protected override async Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameters();

            if (_shouldRender)
            {
                if (_init || _languageCode != _translationState.ActiveLanguageCode)
                {
                    _init = false;
                    var isLoadClients = _languageCode != _translationState.ActiveLanguageCode;

                    _languageCode = _translationState.ActiveLanguageCode;
                    _contentFilterModel.Sports = await LoadSports();

                    Init(isLoadClients);
                }

                _queryMessage = QueryMessage;
            }
        }

        protected override bool ShouldRender() => _shouldRender;
        protected override void OnAfterRender(bool firstRender) => _shouldRender = false;
        private bool ValidateParameters() => _init || _languageCode != _translationState.ActiveLanguageCode || _queryMessage != QueryMessage;

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

            _modalService.Open<CopyClientSettingsModal>($"Copy Client Settings", parameters);
        }

        public void HandleCopyClientSettingsAlert(AlertModel alert)
        {
            _loadFailureShown = false;
            _alertModel = alert;

            Init();
        }

        public void HandleNavMenuCallback() => CalculateConfigCounts();

        public void Dispose()
        {
            _cts?.Dispose();
        }

        #endregion

        #region Utilities

        private async void Init(bool isLoadClient = true)
        {
            CalculateConfigCounts();

            _isPreloader = true;
            _contentFilterModel = new ConfigContentFilterModel();
            _contentFilterModel.Scopes = _scopes;

            var tasks = new List<Task>()
            {
                Task.Run(async () => { _contentFilterModel.Sports = await LoadSports(); _shouldRender = true;})
            };

            if (isLoadClient || _contentFilterModel.Clients == null)
                tasks.Add(Task.Run(async () => { _contentFilterModel.Clients = await LoadClients(); _shouldRender = true; }));

            await Task.WhenAll(tasks.ToArray());

            _isPreloader = false;
            _shouldRender = true;
        }
        private async void CalculateConfigCounts()
        {
            _navMenuParamModel.MyTemplatesCount = ConfigNavMenuParamModel.CountLoading;
            _navMenuParamModel.AssignedLeaguesCount = ConfigNavMenuParamModel.CountLoading;
            _navMenuParamModel.UnAssignedLeaguesCount = ConfigNavMenuParamModel.CountLoading;

            if (_loadFailureShown)
            {
                _alertModel = new AlertModel();
                _loadFailureShown = false;
            }

            _shouldRender = true;

            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            await ExecuteWithCancellation(async () =>
            {
                var response = await _backofficeService.GetTemplateConfigCount(token: token);

                if (response.StatusCode == StatusCode.Success)
                {
                    _navMenuParamModel.MyTemplatesCount = response.Result;
                }
                else
                {
                    _navMenuParamModel.MyTemplatesCount = ConfigNavMenuParamModel.CountFailed;
                    ReportLoadFailure("template count", response.Message);
                }

                _shouldRender = true;
                StateHasChanged();
            }, token, () => _navMenuParamModel.MyTemplatesCount = ConfigNavMenuParamModel.CountFailed);

            await ExecuteWithCancellation(async () =>
            {
                var response = await _backofficeService.GetTemplateConfigCount(ConfigState.Assigned.ToString(), token);

                if (response.StatusCode == StatusCode.Success)
                {
                    _navMenuParamModel.AssignedLeaguesCount = response.Result;
                }
                else
                {
                    _navMenuParamModel.AssignedLeaguesCount = ConfigNavMenuParamModel.CountFailed;
                    ReportLoadFailure("assigned count", response.Message);
                }

                _shouldRender = true;
                StateHasChanged();
            }, token, () => _navMenuParamModel.AssignedLeaguesCount = ConfigNavMenuParamModel.CountFailed);

            await ExecuteWithCancellation(async () =>
            {
                var attempt = 0;
                string? lastMessage = null;
                while (attempt < 3)
                {
                    var response = await _backofficeService.GetTemplateConfigCount(ConfigState.UnAssigned.ToString(), token);

                    if (response.StatusCode == StatusCode.Success)
                    {
                        _navMenuParamModel.UnAssignedLeaguesCount = response.Result;
                        break;
                    }

                    lastMessage = response.Message;
                    attempt++;
                }

                if (attempt == 3)
                {
                    _navMenuParamModel.UnAssignedLeaguesCount = ConfigNavMenuParamModel.CountFailed;
                    ReportLoadFailure("unassigned count", lastMessage);
                }

                _shouldRender = true;
                StateHasChanged();
            }, token, () => _navMenuParamModel.UnAssignedLeaguesCount = ConfigNavMenuParamModel.CountFailed);
        }

        private async Task ExecuteWithCancellation(Func<Task> action, CancellationToken token, Action? onFailure = null)
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
                ReportLoadFailure("page data", ex.Message, ex);
                onFailure?.Invoke();
                _shouldRender = true;
                StateHasChanged();
            }
        }

        // The Web service turns a failed call into an Error response. Show it, instead of leaving "Calculating..." or
        // an empty list that reads as "no data". The API message goes to the log, not to the screen.
        private void ReportLoadFailure(string what, string? apiMessage, Exception? exception = null)
        {
            if (exception != null)
                Log.Error(exception, "OE Config could not load the {What}: {Message}", what, apiMessage);
            else
                Log.Warning("OE Config could not load the {What}: {Message}", what, apiMessage);

            _loadFailureShown = true;
            _alertModel = new AlertModel
            {
                Status = AlertStatus.Error,
                Message = "Some of the Config information could not be loaded. Please try again, or contact support if it keeps happening.",
                IsAlwaysShown = true
            };
        }
        private async Task<List<SelectListItemModel<string>>> LoadSports()
        {
            var response = await _backofficeService.GetSportLookup(_languageCode.ToString());

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
            {
                ReportLoadFailure("client list", response.Message);
                return new List<SelectListItemWithCheckboxModel<string>>();
            }

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
