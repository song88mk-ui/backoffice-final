using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Pages.SBC;
using Backoffice.Web.Services;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Serilog;

namespace Backoffice.Web.Components.SBC.Announcements
{
    public partial class CreateOrUpdateModal : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        private EditContext EditContext { get; set; }
        
        #endregion

        #region Fields

        private bool _isEdit;
        private bool _isPreload;
        private bool _init = true;
        private bool _shouldRender;

        private decimal _opacity;
        private LanguageCode _selectedLanguage;

        private List<AlertModel> _alerts;
        private CancellationTokenSource _cts;
        private List<LanguageCode> _languageCodes;
        private Models.Announcements _announcement;
        private Models.Announcements _prevAnnouncement;
        private List<SelectListItemWithCheckboxModel<string>> _clients;
        private Dictionary<LanguageCode, string> _announcementMessages;
        private List<string> _selectedClientCodes;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _isPreload = false;

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
                Languages = _announcement.Languages.Select(l => new AnnouncementMessage { LanguageCode = l.LanguageCode, Message = l.Message}).ToList(),
            };

            var clients = ModalParameters.TryGet <List<SelectListItemWithCheckboxModel<string>>>("Clients");
            _isEdit = ModalParameters.TryGet<bool>("IsEdit");
            _languageCodes = Enum.GetValues<LanguageCode>().Select(s => s).ToList();
            _selectedLanguage = _announcement.LanguageCode;

            _announcementMessages = new Dictionary<LanguageCode, string>();
            foreach(var language in _languageCodes)
            {
                _announcementMessages[language] = string.Empty;

                foreach (var message in _announcement.Languages)
                {
                    if (language == message.LanguageCode)
                    {
                        _announcementMessages[language] = message.Message ?? string.Empty;
                        break;
                    }
                }
            }

            _clients = clients.Select(s => new SelectListItemWithCheckboxModel<string>
            {
                Value = s.Value,
                Name = s.Name,
                IsChecked = _isEdit ? _announcement.Clients.Select(s => s.ClientCode).Contains(s.Value) : true,
                IsSelectAll = s.IsSelectAll
            }).ToList();

            _selectedClientCodes =_clients.Where(c => c.IsChecked).Select(s => s.Value).ToList();

            EditContext = new EditContext(_announcement);
        }

        protected override Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameters();

            _init = false;
            return base.OnParametersSetAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            _shouldRender = false;
        }

        protected override bool ShouldRender() => _shouldRender;

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
            _announcement.Languages = _prevAnnouncement.Languages;

            ModalService.Cancel();

            _shouldRender = true;
        }

        public async Task OnSaveClickEvent()
        {
            try
            {
                _cts = new CancellationTokenSource();

                if (_isPreload)
                    return;

                _isPreload = true;

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

                if (!_announcementMessages.Any(m => !string.IsNullOrEmpty(m.Value)))
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

                if (!_selectedClientCodes.Any())
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

                var request = new AddOrUpdateAnnouncementRequest
                {
                    Id = _announcement.Id,
                    Title = _announcement.Title,
                    StartDate = _announcement.StartDate,
                    EndDate = _announcement.EndDate,
                    IsActive = _announcement.IsActive,
                    AnnouncementMessages = _announcementMessages.Where(m => !string.IsNullOrEmpty(m.Value)).Select(s => new AnnouncementMessage { LanguageCode = s.Key, Message = s.Value }).ToList(),
                    ClientCodes = _selectedClientCodes
                };

                if(_isEdit)
                {
                    _announcement.Message = _announcementMessages[_announcement.LanguageCode].ToString();
                    _announcement.Clients = _selectedClientCodes.Select(s => new AnnouncementResponse.ClientInfo { ClientCode = s }).ToList();
                    _announcement.Languages = request.AnnouncementMessages;
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
            _shouldRender = true;
        }

        public async Task OnLanguageClicked(LanguageCode languageCode)
        {
            if (_isPreload)
                return;

            _selectedLanguage = languageCode;
            _shouldRender = true;
        }

        public async Task OnClientCheckboxClickEvent(string selectedItems)
        {
            _selectedClientCodes = selectedItems.Split(",").ToList();
            _shouldRender = true;
        }

        public void Dispose()
        {

        }

        #endregion

        #region Utilities

        private bool ValidateParameters()
        {
            return _init;
        }

        #endregion
    }
}
