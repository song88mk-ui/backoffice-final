using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.OddsEngine.Configuration;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Config;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ConfigMyTemplates : IDisposable
    {
        #region Properties
        [Inject] private IJSRuntime JSRuntime { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        [Parameter] public ConfigContentFilterModel FilterModel { get; set; }

        [Parameter] public EventCallback OnStateHasChanged { get; set; }
        [Parameter] public string QueryMessage { get; set; }

        private bool _isPreloader;
        private int _selectedSportId;
        private int _selectedScopeId;
        private SearchFormModel _searchForm = new();
        private AlertModel _alertModel;

        private string _pageTitle = "My Templates";
        private List<BreadcrumbModel> _breadcrumbs;
        private List<SelectListItemWithCheckboxModel<string>> _clients = new();
        private List<SelectListItemModel<string>> _sports = new();
        private List<SelectListItemModel<string>> _scopes = new();

        private List<ConfigMyTemplateModel>? _configTemplates = null;

        private CancellationTokenSource _cancellationToken;
        private LanguageCode _languageCode;
        private bool _init = true;
        private bool _shouldRender = true;
        #endregion

        #region Methods
        protected override void OnInitialized() => _breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = "hk/odds-engine/config/my-templates",
                    IsCurrentPage = true
                }
            };
        protected override async Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameters();

            if (_shouldRender)
            {
                _languageCode = TranslationState.ActiveLanguageCode;
                if(FilterModel?.Clients != null)
                _clients = new List<SelectListItemWithCheckboxModel<string>>(FilterModel.Clients);
                if (FilterModel?.Sports != null)
                    _sports = new List<SelectListItemModel<string>>(FilterModel.Sports);
                if (FilterModel?.Scopes != null)
                    _scopes = new List<SelectListItemModel<string>>(FilterModel.Scopes);

                _selectedSportId = -1;
                _selectedScopeId = int.Parse(_scopes.FirstOrDefault()?.Value ?? "-1");

                _alertModel = new AlertModel();
                _alertModel.Message = QueryMessage;
                _alertModel.Status = AlertStatus.Success;
                _alertModel.Opacity = 1.0m;

                await LoadConfigTemplates();
            }
        }
        protected override bool ShouldRender() => _shouldRender;
        protected override void OnAfterRender(bool firstRender) => _shouldRender = false;
        private bool ValidateParameters() => _init || _languageCode != TranslationState.ActiveLanguageCode || !_clients.SequenceEqual(FilterModel.Clients) || !_sports.SequenceEqual(FilterModel.Sports) || !_scopes.SequenceEqual(FilterModel.Scopes) || (QueryMessage != null && !string.IsNullOrEmpty(QueryMessage) && _alertModel.Message != QueryMessage);


        public void OnEditTemplateClickEvent(ConfigTemplateModel configTemplateModel) => _navigationManager.NavigateTo($"/hk/odds-engine/config-templates/edit/{configTemplateModel.Id}");
        public void OnCopyTemplateClickEvent(ConfigTemplateModel configTemplateModel) => _navigationManager.NavigateTo($"/hk/odds-engine/config-templates/copy/{configTemplateModel.Id}");

        public void OnLogsClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("FilterModel", FilterModel);
            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-lg modal-dialog-centered");
            parameters.Add("DialogStyle", "max-width:1500px");

            var options = new ModalOptions();

            _modalService.Open<ViewMyTemplatesLogs>("View Logs", parameters, options);
        }
        public void OnCreateNewTemplateClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("FilterModel", FilterModel);
            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-md modal-dialog-centered");

            var options = new ModalOptions();

            _modalService.Open<CreateTemplateModal>($"Create Template", parameters, options);
        }

        public void OnModalClicked(ModalParameters parameters)
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

        public async Task OnSportChangedEvent(ChangeEventArgs e)
        {
            _selectedSportId = int.Parse(e.Value.ToString());

            await LoadConfigTemplates();
        }
        public async Task OnScopeChangedEvent(ChangeEventArgs e)
        {
            _selectedScopeId = int.Parse(e.Value.ToString());

            await LoadConfigTemplates();
        }
        public async Task OnModalSaveEvent(object e)
        {
            await OnStateHasChanged.InvokeAsync();

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

            Task task = LoadConfigTemplates();
        }

        public async Task OnInputSearchEvent() => await LoadConfigTemplates();

        public void Dispose()
        {

        }

        #endregion

        #region Utilities

        private async Task LoadConfigTemplates()
        {
            _cancellationToken?.Cancel();

            _cancellationToken = new CancellationTokenSource();

            if (!_isPreloader)
            {
                _isPreloader = true;
                _shouldRender = true;
                await InvokeAsync(StateHasChanged);
            }

            try
            {
                var token = _cancellationToken.Token;

                var configTemplateResponse = await _backofficeService.GetConfigTemplate(
                    _searchForm.SearchText,
                    _selectedSportId,
                    _selectedScopeId,
                    token);

                if (token.IsCancellationRequested)
                    return;

                _configTemplates = configTemplateResponse?.Result?.Select(config => new ConfigMyTemplateModel()
                {
                    Id = config.Id,
                    Name = config.Name,
                    SportName = _sports.FirstOrDefault(e => e.Value == config.SportId.ToString()) != null ? $"{_sports.FirstOrDefault(e => e.Value == config.SportId.ToString())?.Name ?? ""}" : "",
                    ClientAssigned = $"{config.ClientsCount} Client{(config.ClientsCount > 1 ? "s" : "")}",
                    LeagueAssigned = $"{config.LeaguesCount} League{(config.LeaguesCount > 1 ? "s" : "")}",
                    DateCreated = config.Timestamp.ToString("HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture),
                    IsReadOnly = !(config.ClientsCount > 0 && config.LeaguesCount > 0)
                }).ToList() ?? new List<ConfigMyTemplateModel>();

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