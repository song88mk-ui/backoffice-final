using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Diagnostics;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class Filter2
    {
        [Inject] public IPreRenderService PreRenderService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }

        [CascadingParameter(Name = "UserClientCodes")] public List<string>? UserClientCodes { get; set; }

        [Parameter] public EventCallback<string> OnClientChangedCallback { get; set; }
        [Parameter] public EventCallback OnCopyClickCallback { get; set; }
        [Parameter] public EventCallback<Tuple<bool, EditSportSettingsPage>> OnUpdateClickCallback { get; set; }
        [Parameter] public EventCallback<Tuple<bool, EditSportSettingsPage>> OnCancelClickEvent { get; set; }
        [Parameter] public bool Render { get; set; }
        [Parameter] public bool IsPreload { get; set; }

        private List<SelectListItemModel<string>> _clients = new List<SelectListItemModel<string>>();

        private EditSportSettingsPage _editSportSettingsPage;

        private string _selectedClientCode;

        private int _counter;

        private bool _isUpdate;
        private bool _init;
        private bool _shouldRender;

        protected override async Task OnInitializedAsync()
        {
            if (PreRenderService.IsPreRendering)
                return;

            _init = true;

            TradingService.Region = "HK";

            if (UserClientCodes != null && UserClientCodes.Any())
            {
                var clientResponse = await TradingService.GetClientList();

                if (clientResponse.StatusCode == StatusCode.Success)
                {
                    clientResponse.Result = clientResponse.Result.Where(c => UserClientCodes.Contains(c.ClientCode.ToUpper())).ToList();

                    _clients = clientResponse.Result.Select(s => new SelectListItemModel<string>
                    {
                        Value = s.ClientCode,
                        Name = s.ClientName,
                    }).OrderBy(x => x.Name).ToList();

                    _selectedClientCode = _clients.FirstOrDefault().Value;
                    await OnClientChangedCallback.InvokeAsync(_selectedClientCode);
                }
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            _shouldRender = _init ? _init : Render;

            if (!_shouldRender)
                return;

            Debug.WriteLine($"Filter2: {_counter++}");

            _init = false;
        }

        protected override bool ShouldRender() => _shouldRender;

        public async void OnClientChangedEvent(ChangeEventArgs e)
        {
            _selectedClientCode = e.Value.ToString();
            await OnClientChangedCallback.InvokeAsync(_selectedClientCode);

            if (_isUpdate)
            {
                _isUpdate = false;
                _editSportSettingsPage = EditSportSettingsPage.Main;

                await OnUpdateClickCallback.InvokeAsync(new Tuple<bool, EditSportSettingsPage>(_isUpdate, _editSportSettingsPage));
            }
        }

        public async Task OnUpdateClick()
        {
            _isUpdate = true;
            _editSportSettingsPage = EditSportSettingsPage.Sport;

            await OnUpdateClickCallback.InvokeAsync(new Tuple<bool, EditSportSettingsPage>(_isUpdate, _editSportSettingsPage));

            _shouldRender = true;
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
            await OnCancelClickEvent.InvokeAsync();
        }
        public async Task OnConfirmationModalSaveClicked()
        {
            _isUpdate = _editSportSettingsPage == EditSportSettingsPage.SegmentAndLeagues ? true : false;
            _editSportSettingsPage = EditSportSettingsPage.Sport;

            await OnUpdateClickCallback.InvokeAsync(new Tuple<bool, EditSportSettingsPage>(_isUpdate, _editSportSettingsPage));

            _shouldRender = true;
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnSaveChangesClick()
        {
            if (_editSportSettingsPage == EditSportSettingsPage.Sport)
            {
                _isUpdate = true;
                _editSportSettingsPage = EditSportSettingsPage.SegmentAndLeagues;

                await OnUpdateClickCallback.InvokeAsync(new Tuple<bool, EditSportSettingsPage>(_isUpdate, _editSportSettingsPage));

                _shouldRender = false;
            }
            else if (_editSportSettingsPage == EditSportSettingsPage.SegmentAndLeagues)
            {
                _isUpdate = false;
                _editSportSettingsPage = EditSportSettingsPage.Main;

                _shouldRender = true;

                await OnUpdateClickCallback.InvokeAsync(new Tuple<bool, EditSportSettingsPage>(_isUpdate, _editSportSettingsPage));

            }
        }
    }
}
