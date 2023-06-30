using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ModifyLeagueTemplateModal : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackOfficeService BackOfficeService { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        [Parameter] public EventCallback<object> OnSaveClickCallback { get; set; }

        public List<ConfigClientViewModel> Leagues { get; set; }
        public List<SelectListItemModel<long>> Templates { get; set; }

        public SelectListItemModel<long> SelectedItem { get; set; }

        public bool IsRemoveCurrentTemplate { get; set; } = false;

        private ConfigState State = ConfigState.Assigned;
        private bool IsPreLoader = false;
        private bool IsShowAll = false;        
        private string ActivityLogPayload = "";

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {

            IsPreLoader = true;
            Templates = new List<SelectListItemModel<long>>();
            Leagues = new List<ConfigClientViewModel>();

            Leagues = ModalParameters.TryGet<List<ConfigClientViewModel>>("Leagues");
            State = ModalParameters.TryGet<ConfigState>("State");
            var sportId = ModalParameters.TryGet<int>("SportId");
            var scopeId = ModalParameters.TryGet<int>("ScopeId");
            ActivityLogPayload = $"{sportId}.{scopeId}";
            Templates = await GetTemplates(sportId, scopeId);
        }

        public async Task OnTemplateSelectChangedv2(ConfigTemplateModel e)
        {
            var templateId = e.Id;
        }
        public async Task OnTemplateSelectChanged(SelectListItemModel<long> e)
        {
            SelectedItem = new SelectListItemModel<long>();
            SelectedItem = e;

            IsRemoveCurrentTemplate = false;

            if (SelectedItem != null && SelectedItem.Value == -1)
            {
                IsRemoveCurrentTemplate = true;
            }            
        }
        public async Task OnRemoveTemplateClickedEvent()
        {

        }

        public async Task OnCancelClickedEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnSaveClickedEvent()
        {
            IsPreLoader = true;
            StateHasChanged();

            if (SelectedItem != null)
            {
                var leagueRequest = Leagues.Select(x => new ConfigClientModel()
                {
                    Id = x.Id,
                    ClientId = x.ClientId,
                    ClientCode = x.ClientCode,
                    ClientName = x.ClientName,
                    SportId = x.SportId,
                    SportName = x.SportName,
                    FixtureStatusId = x.FixtureStatusId,
                    FixtureStatusName = x.FixtureStatusName,
                    SegmentId = x.SegmentId,
                    SegmentName = x.SegmentName,
                    LeagueId = x.LeagueId,
                    LeagueName = x.LeagueName,
                    TemplateId = x.TemplateId,
                    TemplateName = x.TemplateName
                }).ToList();
                var state = ConfigState.UnAssigned;

                if (SelectedItem.Value > 0)
                {
                    state = ConfigState.Assigned;
                }

                var response = await BackOfficeService.SetLeagueState(state, new ConfigSetLeagueStateRequest()
                {
                    TemplateId = SelectedItem.Value,
                    Leagues = leagueRequest
                });

                if (response.StatusCode == StatusCode.Success)
                {
                    var distinctTemplates = Leagues.Select(x => new
                    {
                        Id = x.TemplateId.ToString(),
                        Name = x.TemplateName
                    }).Distinct().ToList();

                    var cancellationTokenSource = new CancellationTokenSource();

                    Parallel.ForEach(distinctTemplates, async (item) =>
                    {
                        var leagues = Leagues.Where(x => x.TemplateId.ToString() == item.Id).ToList();

                        var from = "";
                        var to = "";
                        from = JsonSerializer.Serialize(new ConfigTemplateLogModel()
                        {
                            TemplateName = item.Id != "-1" ? item.Name : "Unassigned",
                            Payloads =  new ConfigTemplateLogPayloadModel()
                            {
                                Clients = leagues.Select(x => x.ClientName).Distinct().ToList(),
                                Leagues = leagues.Select(x => x.LeagueName).Distinct().ToList()
                            }
                        });
                        to = JsonSerializer.Serialize(new ConfigTemplateLogModel()
                        {
                            TemplateName = SelectedItem.Value.ToString() != "-1" ? SelectedItem.Name : "Unassigned",
                            Payloads =  new ConfigTemplateLogPayloadModel()
                            {
                                Clients = new List<string>(),
                                Leagues = new List<string>()
                            }
                        });

                        var activityLog = new ActivityLogModel()
                        {
                            Page = SportsbookClientPermission.SBC_OE_Config.ToString().Replace("_", "."),
                            Payload = $"{ActivityLogPayload}.{SelectedItem.Name}.T".Replace(" ", "_"),
                            From = from,
                            To = to,
                            Activity = Activity.Update.ToString(),
                            UserId = UserModel.Id,
                            UserName = UserModel.UserName,
                            TimeStamp = DateTime.UtcNow
                        };

                        await BackOfficeService.AddActivityLog(activityLog);
                    });

                    await Modal.OnSaveClickCallback.InvokeAsync();
                }

            }

            IsPreLoader = true;
            ModalService.Cancel();
        }

        public async Task OnShowAllClickedEvent()
        {
            IsShowAll = !IsShowAll;
        }

        public void Dispose()
        {

        }
        #endregion

        #region Utilities

        private async Task<List<SelectListItemModel<long>>> GetTemplates(int sportId, int scopeId)
        {
            IsPreLoader = true;
            var response = await BackOfficeService.GetConfigTemplate(sportId, scopeId);

            IsPreLoader = false;
            if (response.StatusCode == StatusCode.Success && response.Result.Count > 0)
            {
                return response.Result.Select(x => new SelectListItemModel<long>()
                {
                    Value = x.Id,
                    Name = x.Name
                }).ToList();

            }

            return new List<SelectListItemModel<long>>()
            {
                new SelectListItemModel<long>()
                {
                    Value =- 1,
                    Name = "No Template"
                }
            };
        }

        #endregion
    }
}
