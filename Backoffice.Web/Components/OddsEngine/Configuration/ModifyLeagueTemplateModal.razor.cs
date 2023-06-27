using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using RabbitMQ.Client.Apigen.Attributes;

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
        private ConfigState State = ConfigState.Assigned;
        private bool IsPreLoader = false;
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
            Templates = await Gettemplates(sportId, scopeId);
        }

        public async Task OnTemplateSelectChangedv2(ConfigTemplateModel e)
        {
            var templateId = e.Id;
        }
        public async Task OnTemplateSelectChanged(SelectListItemModel<long> e)
        {
            SelectedItem = new SelectListItemModel<long>();
            SelectedItem = e;
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

                if (SelectedItem.Value > 0)
                {
                    var response = await BackOfficeService.SetLeagueState(ConfigState.Assigned, new ConfigSetLeagueStateRequest()
                    {
                        TemplateId = SelectedItem.Value,
                        Leagues = leagueRequest
                    });
                }
                else
                {
                    var response = await BackOfficeService.SetLeagueState(ConfigState.Unsigned, new ConfigSetLeagueStateRequest()
                    {
                        TemplateId = SelectedItem.Value,
                        Leagues = leagueRequest
                    });
                }

                await Modal.OnSaveClickCallback.InvokeAsync();
            }

            IsPreLoader = true;
            ModalService.Cancel();
        }

        public void Dispose()
        {

        }
        #endregion

        #region Utilities

        private async Task<List<SelectListItemModel<long>>> Gettemplates(int sportId, int scopeId)
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
