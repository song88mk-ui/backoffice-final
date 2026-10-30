using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.OddsEngine.Configuration;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Config;
using Microsoft.AspNetCore.Components;
using Serilog;
using System.Globalization;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ConfigMyTemplates : IDisposable
    {
        #region Properties
        [Parameter] public ConfigContentFilterModel FilterModel { get; set; }
        [Parameter] public EventCallback OnStateHasChange { get; set; }
        [Parameter] public string QueryMessage { get; set; }

        private bool _isPreloader;
        private int _selectedSportId;
        private int _selectedScopeId;
        private SearchFormModel _searchForm = new();
        private AlertModel _alertModel;
        private bool _loadFailureShown;

        private string _pageTitle = "My Templates";

        private List<ConfigMyTemplateModel>? _configTemplates = null;

        private CancellationTokenSource? _cts;
        private LanguageCode _languageCode;
        private bool _init = true;
        private bool _shouldRender = true;

        #endregion

        #region Methods
        protected override async Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameters();

            if (_shouldRender)
            {
                _languageCode = _translationState.ActiveLanguageCode;
                _selectedSportId = -1;
                _selectedScopeId = int.Parse(FilterModel.Scopes.FirstOrDefault()?.Value ?? "-1");

                _alertModel = new AlertModel();
                _alertModel.Message = QueryMessage;
                _alertModel.Status = AlertStatus.Success;
                _alertModel.Opacity = 1.0m;

                if (_init)
                    await LoadConfigTemplates();

            }
            _init = false;
        }

        protected override bool ShouldRender() => _shouldRender;

        protected override void OnAfterRender(bool firstRender) => _shouldRender = false;

        private bool ValidateParameters() => _init || _languageCode != _translationState.ActiveLanguageCode || (!string.IsNullOrEmpty(QueryMessage) && _alertModel.Message != QueryMessage);

        public void HandleLogsClick()
        {
            var parameters = new ModalParameters();

            parameters.Add("FilterModel", FilterModel);
            parameters.Add("Sports", FilterModel.Sports);
            parameters.Add("Scopes", FilterModel.Scopes);
            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-lg modal-dialog-centered");
            parameters.Add("DialogStyle", "max-width:1500px");

            var options = new ModalOptions();

            _modalService.Open<ViewMyTemplatesLogs>("View Logs", parameters, options);
        }
        public void HandleNewTemplateClick()
        {
            var parameters = new ModalParameters();

            parameters.Add("FilterModel", FilterModel);
            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-md modal-dialog-centered");

            var options = new ModalOptions();

            _modalService.Open<CreateTemplateModal>($"Create Template", parameters, options);
        }

        public void HandleModalClick(ModalParameters parameters)
        {
            if (parameters != null)
            {
                var type = parameters.TryGet<string>("ModalType");
                var options = new ModalOptions();

                switch (type)
                {
                    case "ExportTemplateModal":
                        _modalService.Open<ExportTemplateModal>($"Export Templates", parameters, options);
                        break;
                    case "ViewAssignedModal":
                        _modalService.Open<ViewAssignedModal>($"View Assigned", parameters, options);
                        break;
                    case "DeleteTemplateModal":
                        _modalService.Open<DeleteTemplateModal>($"Delete Template", parameters, options);
                        break;
                }
            }
        }

        public async Task HandleSportChange(ChangeEventArgs e)
        {
            _selectedSportId = int.Parse(e.Value.ToString());

            await LoadConfigTemplates();
        }
        public async Task HandleScopeChange(ChangeEventArgs e)
        {
            _selectedScopeId = int.Parse(e.Value.ToString());

            await LoadConfigTemplates();
        }
        public async Task HandleModalSave(object e)
        {
            await OnStateHasChange.InvokeAsync();

            if (e is string)
            {
                if (!string.IsNullOrEmpty((string)e))
                {
                    _alertModel = new()
                    {
                        Status = AlertStatus.Success,
                        Message = (string)e
                    };
                }
            }

            await LoadConfigTemplates();
        }

        public async Task HandleSearchInput() => await LoadConfigTemplates();

        public void Dispose()
        {
            _cts?.Dispose();
        }

        #endregion

        #region Utilities

        private async Task LoadConfigTemplates(bool isReload = true)
        {
            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new CancellationTokenSource();

            if (!_isPreloader)
            {
                _isPreloader = true;
                _shouldRender = true;
            }

            try
            {
                var token = _cts.Token;
                var configTemplates = new List<ConfigTemplateModel>();

                if (isReload)
                {
                    var configTemplateResponse = await _backofficeService.GetConfigTemplate(
                        _searchForm.SearchText,
                        _selectedSportId,
                        _selectedScopeId,
                        token);

                    configTemplates = configTemplateResponse?.Result?.ConfigTemplates;

                    if (token.IsCancellationRequested)
                        return;

                    // A failed call must not look like "no templates": say so, and log the API message.
                    if (configTemplateResponse == null || configTemplateResponse.StatusCode != StatusCode.Success)
                    {
                        Log.Warning("OE Config could not load the templates: {Message}", configTemplateResponse?.Message);
                        _loadFailureShown = true;
                        _alertModel = new AlertModel
                        {
                            Status = AlertStatus.Error,
                            Message = "The templates could not be loaded. Please try again, or contact support if it keeps happening.",
                            IsAlwaysShown = true
                        };
                    }
                    else if (_loadFailureShown)
                    {
                        _alertModel = new AlertModel();
                        _loadFailureShown = false;
                    }

                    _configTemplates = configTemplates?.Select(config => new ConfigMyTemplateModel()
                    {
                        Id = config.Id,
                        Name = config.Name,
                        SportId = config.SportId,
                        SportName = FilterModel.Sports.FirstOrDefault(e => e.Value == config.SportId.ToString())?.Name ?? "",
                        ClientAssigned = $"{config.ClientsCount} Client{(config.ClientsCount > 1 ? "s" : "")}",
                        LeagueAssigned = $"{config.LeaguesCount} League{(config.LeaguesCount > 1 ? "s" : "")}",
                        DateCreated = config.Timestamp.ToString("HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture),
                        IsReadOnly = !(config.ClientsCount > 0 && config.LeaguesCount > 0)
                    }).ToList() ?? new();
                }
                else
                {
                    configTemplates = new List<ConfigTemplateModel>();
                    _configTemplates?.ForEach(config => config.SportName = FilterModel.Sports.FirstOrDefault(e => e.Value == config.SportId.ToString())?.Name ?? "");
                }

                if (token.IsCancellationRequested)
                    return;

                _isPreloader = false;
                _shouldRender = true;

            }
            catch (TaskCanceledException)
            {
            }
        }

        #endregion
    }
}