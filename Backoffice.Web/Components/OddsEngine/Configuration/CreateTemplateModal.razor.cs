using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class CreateTemplateModal : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }
        [Inject] private IBackofficeService BackofficeService { get; set; }

        [Parameter] public EventCallback<bool> CreateTemplateCallback { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        private List<SelectListItemModel<int>> _sports;
        private List<SelectListItemModel<int>> _scopes;

        private int _selectedSportId;
        private int _selectedScopeId;
        private string _templateName;
        private bool _isConfigNameExisting;
        private bool _isInvalidName;
        private bool _isPreloader;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _sports = new List<SelectListItemModel<int>>();
            _scopes = new List<SelectListItemModel<int>>()
            {
                new SelectListItemModel<int>()
                {
                    Value = (int)Scope.Prematch,
                    Name = Scope.Prematch.ToString()
                },
                new SelectListItemModel<int>()
                {
                    Value = (int)Scope.Live,
                    Name = Scope.Live.ToString()
                }
            }.OrderBy(x => x.Name).ToList();

            _sports = await LoadSports();
            _selectedSportId = _sports.FirstOrDefault()?.Value ?? -1;
            _selectedScopeId = _scopes.FirstOrDefault()?.Value ?? -1;
        }

        public async Task OnSportSelectChanged(ChangeEventArgs e)
        {
            _selectedSportId = int.Parse(e.Value.ToString());
        }

        public async Task OnScopeSelectChanged(ChangeEventArgs e)
        {
            _selectedScopeId = int.Parse(e.Value.ToString());
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task HandleSaveClick()
        {
            if (string.IsNullOrEmpty(_templateName))
            {
                _isInvalidName = true;
                return;
            }

            _isPreloader = true;
            var isSuccess = false;
            _isConfigNameExisting = false;
            var payload = new ConfigTemplateModel()
            {
                SportId = _selectedSportId,
                FixtureTypeId = (int)FixtureType.Match,
                FixtureStatusId = _selectedScopeId,
                Name = _templateName,
            };

            var response = await BackofficeService.AddOrUpdateConfigTemplate(payload);

            if (response.StatusCode == StatusCode.Success)
            {
                await BackofficeService.AddActivityLog(new ActivityLogModel()
                {
                    Page = SportsbookClientPermission.SBC_OE_Config.ToString().Replace("_", "."),
                    Payload = $"{_selectedSportId}.{_selectedScopeId}.{payload.Name}.T".Replace(" ", "_"),
                    From = "-",
                    To = JsonSerializer.Serialize(payload),
                    Activity = Activity.Create.ToString(),
                    UserId = _userState.Model.Id,
                    UserName = _userState.Model.Name
                });

                isSuccess = true;
                await Modal.OnSaveClickCallback.InvokeAsync($"{_templateName} has successfully been added");
            }
            else
            {
                _isConfigNameExisting = true;
            }

            _isPreloader = false;
            if (isSuccess)
                ModalService.Cancel();
        }
        public async Task HandleNameChange()
        {
            _isInvalidName = false;
            _isConfigNameExisting = false;
        }

        public void Dispose()
        {

        }
        #endregion

        #region Utilities

        private async Task<List<SelectListItemModel<int>>> LoadSports()
        {
            var result = (await TradingService.GetSportLookup(_translationState.ActiveLanguageCode.ToString().ToUpper())).Result;

            if (result == null)
                return new List<SelectListItemModel<int>>();

            var sports = result.SportLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SportId,
                Name = s.SportName
            }).ToList();

            return sports;
        }

        #endregion
    }
}
