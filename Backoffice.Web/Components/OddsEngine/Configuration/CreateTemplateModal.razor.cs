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
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        public List<SelectListItemModel<int>> Sports { get; set; }
        public List<SelectListItemModel<int>> Scopes { get; set; }

        public int SelectedSportId { get; set; }
        public int SelectedScopeId { get; set; }
        public string TemplateName { get; set; }
        public bool IsConfigNameExisting { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Sports = new List<SelectListItemModel<int>>();
            Scopes = new List<SelectListItemModel<int>>()
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

            Sports = await LoadSports();
            SelectedSportId = Sports.FirstOrDefault()?.Value ?? -1;
            SelectedScopeId = Scopes.FirstOrDefault()?.Value ?? -1;
        }

        public async Task OnSportSelectChanged(ChangeEventArgs e)
        {
            SelectedSportId = int.Parse(e.Value.ToString());
        }

        public async Task OnScopeSelectChanged(ChangeEventArgs e)
        {
            SelectedScopeId = int.Parse(e.Value.ToString());
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnSaveClickEvent()
        {
            IsConfigNameExisting = false;
            var payload = new ConfigTemplateModel()
            {
                SportId = SelectedSportId,
                FixtureTypeId = (int)FixtureType.Match,
                FixtureStatusId = SelectedScopeId,
                Name = TemplateName,
            };

            var response = await BackofficeService.AddOrUpdateConfigTemplate(payload);

            if (response.StatusCode == StatusCode.Success)
            {
                await BackofficeService.AddActivityLog(new ActivityLogModel()
                {
                    Page = SportsbookClientPermission.SBC_OE_Config.ToString().Replace("_", "."),
                    Payload = $"{SelectedSportId}.{SelectedScopeId}.{payload.Name}.T".Replace(" ", "_"),
                    From = "-",
                    To = JsonSerializer.Serialize(payload),
                    Activity = Activity.Create.ToString(),
                    UserId = UserModel.Id,
                    UserName = UserModel.UserName,
                    TimeStamp = DateTime.UtcNow
                });

                await Modal.OnSaveClickCallback.InvokeAsync($"{TemplateName} has successfully been added");
                ModalService.Cancel();
            }
            else
            {
                IsConfigNameExisting = true;
            }
        }

        public void Dispose()
        {
            
        }
        #endregion

        #region Utilities

        private async Task<List<SelectListItemModel<int>>> LoadSports()
        {
            var result = (await TradingService.GetSportLookup(TranslationState.ActiveLanguageCode.ToString().ToUpper())).Result;

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
