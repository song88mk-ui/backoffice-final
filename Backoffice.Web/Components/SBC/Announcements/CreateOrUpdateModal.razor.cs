using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Serilog;
using static Backoffice.Shared.Models.Response.FastOddsFixturesResponse;

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
        private static List<string> SelectedClientCodes { get; set; }
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
        private Models.Announcements _prevAnnouncement;
        private List<string> _clientCodes;
        private Dictionary<LanguageCode, string> _announcementMessages;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _isPreload = false;
            _initSelect = true;
            
            _selectClass = "client-code-select";

            _alerts = new List<AlertModel>();
            _announcement = ModalParameters.TryGet<Models.Announcements>("Announcement");
            _prevAnnouncement = new Models.Announcements
            {
                Id = _announcement.Id,
                Clients = _announcement.Clients.Select(s => new AnnouncementResponse.ClientInfo { ClientCode = s.ClientCode, ClientId = s.ClientId }).ToList(),
                IsActive = _announcement.IsActive,
                Title = _announcement.Title,
                Message = _announcement.Message,
                StartDate = _announcement.StartDate,
                EndDate = _announcement.EndDate,
            };
            _clientCodes = ModalParameters.TryGet<List<string>>("ClientCodes");
            _isEdit = ModalParameters.TryGet<bool>("IsEdit");

            _announcementMessages = new Dictionary<LanguageCode, string>();
            _announcementMessages[_announcement.LanguageCode] = _announcement.Message ?? string.Empty;

            EditContext = new EditContext(_announcement);
            SelectedClientCodes = _announcement.Clients.Select(s => s.ClientCode).ToList();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_initSelect)
            {
                JSRuntime.InvokeVoidAsync("initSelect2", $".{_selectClass}", "OnClientChangedEvent");
                //await JSRuntime.InvokeVoidAsync("enableSelect2", $".{_selectClass}");

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

            _announcement.Clients = _prevAnnouncement.Clients.Select(s => new AnnouncementResponse.ClientInfo { ClientCode = s.ClientCode, ClientId = s.ClientId }).ToList();
            _announcement.IsActive = _prevAnnouncement.IsActive;
            _announcement.Title = _prevAnnouncement.Title;
            _announcement.Message = _prevAnnouncement.Message;
            _announcement.StartDate = _prevAnnouncement.StartDate;
            _announcement.EndDate = _prevAnnouncement.EndDate;

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

                if (string.IsNullOrEmpty(_announcement.Title))
                {
                    _alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Title is required",
                        Opacity = _opacity
                    });
                }

                if (string.IsNullOrEmpty(_announcement.Message))
                {
                    _alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Message is required",
                        Opacity = _opacity
                    });
                }

                if (_announcement.StartDate > _announcement.EndDate)
                {
                    _alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Start Date should be less than End Date",
                        Opacity = _opacity
                    });
                }

                if (!SelectedClientCodes.Any())
                {
                    _alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Please assign at least 1 client",
                        Opacity = _opacity
                    });
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
                    ClientCodes = SelectedClientCodes
                };

                if(_isEdit)
                {
                    _announcement.Clients = SelectedClientCodes.Select(s => new AnnouncementResponse.ClientInfo { ClientCode = s }).ToList();
                }

                var response = await BackofficeService.AddOrUpdateAnnouncements(request, token);

                if (response.StatusCode == StatusCode.Success)
                {
                    ModalService.Cancel();

                    await Modal.OnSaveClickCallback.InvokeAsync(_isEdit ? true : response.Result);
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

            if (model.IsSelect)
            {
                if (!SelectedClientCodes.Any(c => c == clientCode))
                    SelectedClientCodes.Add(clientCode);
            }
            else
            {
                var client = SelectedClientCodes.FirstOrDefault(c => c == clientCode);
                if (client != null)
                {
                    SelectedClientCodes.Remove(client);
                }
            }
        }

        public void Dispose()
        {

        }

        #endregion

        #region Utilities

        #endregion
    }
}
