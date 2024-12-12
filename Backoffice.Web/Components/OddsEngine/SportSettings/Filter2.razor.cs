using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Diagnostics;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class Filter2
    {
        //[Parameter] public bool Render { get; set; }
        [Parameter] public bool IsUpdate { get; set; }
        [Parameter] public bool IsPreload { get; set; }
        [Parameter] public EventCallback<string> OnClientChangedCallback { get; set; }
        [Parameter] public EventCallback OnCopyClickCallback { get; set; }
        [Parameter] public EventCallback<EditSportSettingsPage> OnUpdateClickCallback { get; set; }
        [Parameter] public EventCallback OnCancelClickCallback { get; set; }
        [Parameter] public EventCallback OnViewLogsCallback { get; set; }

        private List<SelectListItemModel<string>> _clients = new List<SelectListItemModel<string>>();

        private EditSportSettingsPage _editSportSettingsPage;

        private string _selectedClientCode;

        private int _counter;
        private LanguageCode _languageCode;

        //private bool _init;
        //private bool _shouldRender;

        protected override async Task OnInitializedAsync()
        {
            if (PreRenderService.IsPreRendering)
                return;

            //_init = true;
            var clientResponse = await _tradingService.GetClientList();

            if (clientResponse.StatusCode == StatusCode.Success)
            {
                clientResponse.Result = clientResponse.Result.Where(c => _userState.TradingClients.Any(tc => tc.ClientCode.Equals(c.ClientCode, StringComparison.OrdinalIgnoreCase))).ToList();

                _clients = clientResponse.Result.Select(s => new SelectListItemModel<string>
                {
                    Value = s.ClientCode,
                    Name = s.ClientName,
                }).OrderBy(x => x.Name).ToList();

                _selectedClientCode = _clients.FirstOrDefault()?.Value;
                await OnClientChangedCallback.InvokeAsync(_selectedClientCode);

            }
        }

        //protected override bool ShouldRender() => _shouldRender;

        public async void OnClientChangedEvent(ChangeEventArgs e)
        {
            _selectedClientCode = e.Value.ToString();
            _editSportSettingsPage = EditSportSettingsPage.Main;
            await OnClientChangedCallback.InvokeAsync(_selectedClientCode);

            //if (IsUpdate)
            //{
            //    IsUpdate = false;
            //    _editSportSettingsPage = EditSportSettingsPage.Main;

            //    await OnUpdateClickCallback.InvokeAsync(_editSportSettingsPage);
            //}
        }

        public async Task OnUpdateClick()
        {
            _editSportSettingsPage = EditSportSettingsPage.Sport;

            await OnUpdateClickCallback.InvokeAsync(_editSportSettingsPage);

            //_shouldRender = true;
        }
        public async Task OnCopyClickEvent()
        {
            if (IsPreload)
                return;

            if (string.IsNullOrEmpty(_selectedClientCode))
                return;

            await OnCopyClickCallback.InvokeAsync();
        }
        public async Task OnCancelClick()
        {
            if (_editSportSettingsPage == EditSportSettingsPage.SegmentAndLeagues)
            {
                _editSportSettingsPage = EditSportSettingsPage.Sport;
                await OnUpdateClickCallback.InvokeAsync(_editSportSettingsPage);
            }
            else
            {
                await OnCancelClickCallback.InvokeAsync();
            }
        }

        public async Task OnSaveChangesClick()
        {
            if (_editSportSettingsPage == EditSportSettingsPage.Sport)
            {
                _editSportSettingsPage = EditSportSettingsPage.SegmentAndLeagues;

                await OnUpdateClickCallback.InvokeAsync(_editSportSettingsPage);
            }
            else if (_editSportSettingsPage == EditSportSettingsPage.SegmentAndLeagues)
            {
                _editSportSettingsPage = EditSportSettingsPage.Main;
                await OnUpdateClickCallback.InvokeAsync(_editSportSettingsPage);

            }
        }
    }
}
