using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Pages.OddsEngine;
using Backoffice.Web.Services;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Serilog;

namespace Backoffice.Web.Components.SBC.Announcements
{
    public partial class CreateOrUpdateModal : IDisposable
    {
        #region Properties

        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        private EditContext EditContext { get; set; }
        #endregion

        #region Fields
        private bool _isEdit;
        private bool _isPreload;
        private bool _initSelect;
        private string _selectClass;
        private decimal _opacity;
        private List<AlertModel> _alerts;
        private CancellationTokenSource _cts;
        private Models.Announcements _announcement;
        private List<string> _clientCodes;
        private Dictionary<LanguageCode, string> _announcementMessages;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _isPreload = false;
            _initSelect = true;
            
            _selectClass = "client-select";

            _alerts = new List<AlertModel>();
            _announcement = ModalParameters.TryGet<Models.Announcements>("Announcement");
            _clientCodes = ModalParameters.TryGet<List<string>>("ClientCodes");
            _isEdit = ModalParameters.TryGet<bool>("IsEdit");

            _announcementMessages = new Dictionary<LanguageCode, string>();
            _announcementMessages[_announcement.LanguageCode] = _announcement.Message ?? string.Empty;

            EditContext = new EditContext(_announcement);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_initSelect)
            {
                await JSRuntime.InvokeVoidAsync("initSelect2", $".{_selectClass}", "OnClientChangedEvent");
                await JSRuntime.InvokeVoidAsync("enableSelect2", $".{_selectClass}");

                _initSelect = false;
            }
        }

        public async Task OnStatusClickEvent()
        {
            _announcement.IsActive = !_announcement.IsActive;
        }

        public async Task OnRangeSelectStartDate(DateRange range)
        {
            _announcement.StartDate = range.Start.LocalDateTime;
        }

        public async Task OnRangeSelectEndDate(DateRange range)
        {
            _announcement.EndDate = range.Start.LocalDateTime;
        }

        public async Task OnCancelClickEvent()
        {
            if (_isPreload)
                return;

            ModalService.Cancel();
        }

        public async Task OnSaveClickEvent()
        {
            try
            {
                _cts = new CancellationTokenSource();

                if (_isPreload)
                    return;

                _isPreload = true;
                StateHasChanged();

                _alerts.Clear();

                _opacity = _opacity == 1m ? 0.99m : 1m;

                if (!EditContext.Validate())
                {
                    foreach (var message in EditContext.GetValidationMessages().ToList())
                    {
                        if (!_alerts.Any(a => a.Message == message))
                        {
                            _alerts.Add(new AlertModel
                            {
                                Status = AlertStatus.Error,
                                Message = $"{message}",
                                Opacity = _opacity
                            });
                        }
                    }
                    _isPreload = false;
                    return;
                }

                if (_alerts.Any())
                {
                    _isPreload = false;
                    return;
                }

                var token = _cts.Token;
                //TODO: logic to assign list of Messages
                _announcementMessages[_announcement.LanguageCode] = _announcement.Message ?? string.Empty;

                var request = new AddOrUpdateAnnouncementRequest
                {
                    Id = _announcement.Id,
                    Title = _announcement.Title,
                    StartDate = _announcement.StartDate,
                    EndDate = _announcement.EndDate,
                    IsActive = _announcement.IsActive,
                    AnnouncementMessages = _announcementMessages.Select(s => new AnnouncementMessage { LanguageCode = s.Key, Message = s.Value }).ToList(),
                    ClientCodes = _announcement.Clients.Select(s => s.ClientCode).ToList()
                };

                var response = await BackofficeService.AddOrUpdateAnnouncements(request, token);

                if (response.StatusCode == StatusCode.Success)
                {
                    ModalService.Cancel();

                    await Modal.OnSaveClickCallback.InvokeAsync(_isEdit);
                }
                else
                {
                    _alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = $"{response.Message}",
                        Opacity = _opacity
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }
            _isPreload = false;
        }

        [JSInvokable("OnClientChangedEvent")]
        public static void OnClientChangedEvent(SelectedModel model)
        {
            var clientCode = model.SelectedOptionId;
            //var marketGroup = _marketGroups.FirstOrDefault(c => c.Value == marketGroupId);

            //if (marketGroup != null)
            //{
            //    var marketTemplateId = model.ParentId.Split('-')[1];

            //    if (model.IsSelect)
            //    {
            //        foreach (var market in _marketTemplates.Where(c => c.MarketTemplateId == marketTemplateId))
            //        {
            //            if (!market.MarketTemplateGroupNames.Select(s => s.ToLower()).Contains(marketGroup.Name.ToLower()))
            //                market.MarketTemplateGroupNames.Add(marketGroup.Name);
            //        }
            //    }
            //    else
            //    {
            //        foreach (var market in _marketTemplates.Where(c => c.MarketTemplateId == marketTemplateId))
            //        {
            //            if (market.MarketTemplateGroupNames.Select(s => s.ToLower()).Contains(marketGroup.Name.ToLower()))
            //                market.MarketTemplateGroupNames.Remove(marketGroup.Name);
            //        }
            //    }
            //}
        }

        public void Dispose()
        {

        }

        #endregion

        #region Utilities

        #endregion
    }
}
