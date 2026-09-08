using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Backoffice.Web.Pages.SBC;
using Backoffice.Web.Services;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
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

        private string NO_TITLE_ERR_MSG = "Title is required";
        private string NO_MSG_ERR_MSG = "Message is required";
        private string NO_CLIENT_ERR_MSG = "Please assign at least 1 client";
        private string INVALID_START_DATE_ERR_MSG = "Start Date should be less than End Date";

        private decimal _opacity;

        private LanguageCode _selectedLanguage;

        private CancellationTokenSource _cts;
        private Models.Announcements _announcement;
        private Models.Announcements _prevAnnouncement;

        private List<string> _selectedClientCodes;
        private List<AnnouncementsAlertModel> _alerts;
        private List<SelectListItemWithCheckboxModel<string>> _clients;
        private LanguageOptionsModel _languages;
        private Dictionary<LanguageCode, string> _announcementMessages;
        private DateTime _startDate;
        private DateTime _endDate;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _isPreload = false;

            _startDate = DateTime.UtcNow.AddDays(-1);
            _endDate = DateTime.UtcNow.AddDays(1).AddTicks(-1);

            _alerts = new List<AnnouncementsAlertModel>();
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
                Languages = _announcement.Languages.Select(l => new AnnouncementMessage { LanguageCode = l.LanguageCode, Message = l.Message }).ToList(),
            };

            var clients = ModalParameters.TryGet<List<SelectListItemWithCheckboxModel<string>>>("Clients");
            _isEdit = ModalParameters.TryGet<bool>("IsEdit");

            _languages = new LanguageOptionsModel
            {
                Languages = Enum.GetValues<LanguageCode>().Select(s => new LanguageOptions { Language = s, ClickAction = (_ => OnLanguageClicked(s)) }).ToList()
            };

            _selectedLanguage = _announcement.LanguageCode;

            _announcementMessages = new Dictionary<LanguageCode, string>();
            foreach (var language in _languages.Languages)
            {
                _announcementMessages[language.Language] = string.Empty;

                foreach (var message in _announcement.Languages)
                {
                    if (language.Language == message.LanguageCode)
                    {
                        _announcementMessages[language.Language] = message.Message ?? string.Empty;
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

            _selectedClientCodes = _clients.Where(c => c.IsChecked).Select(s => s.Value).ToList();

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
            _announcement.StartDate = range.Start.UtcDateTime;
            _shouldRender = true;
        }

        public async Task OnRangeSelectEndDate(DateRange range)
        {
            _announcement.EndDate = range.Start.UtcDateTime;
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
                            _alerts.Add(new AnnouncementsAlertModel
                            {
                                AnnouncementAlertType = AnnouncementAlertType.GeneralError,
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
                    _alerts.Add(new AnnouncementsAlertModel
                    {
                        AnnouncementAlertType = AnnouncementAlertType.NoTitle,
                        Status = AlertStatus.Error,
                        Message = NO_TITLE_ERR_MSG,
                        Opacity = _opacity
                    });
                }

                if (!_announcementMessages.Any(m => !string.IsNullOrEmpty(m.Value)))
                {
                    _alerts.Add(new AnnouncementsAlertModel
                    {
                        AnnouncementAlertType = AnnouncementAlertType.NoMessage,
                        Status = AlertStatus.Error,
                        Message = NO_MSG_ERR_MSG,
                        Opacity = _opacity
                    });
                }

                if (_announcement.StartDate > _announcement.EndDate)
                {
                    _alerts.Add(new AnnouncementsAlertModel
                    {
                        AnnouncementAlertType = AnnouncementAlertType.InvalidStartDate,
                        Status = AlertStatus.Error,
                        Message = INVALID_START_DATE_ERR_MSG,
                        Opacity = _opacity
                    });
                }

                if (!_selectedClientCodes.Any() && !_isEdit)
                {
                    _alerts.Add(new AnnouncementsAlertModel
                    {
                        AnnouncementAlertType = AnnouncementAlertType.NoClient,
                        Status = AlertStatus.Error,
                        Message = NO_CLIENT_ERR_MSG,
                        Opacity = _opacity
                    });
                }

                if (_alerts.Any())
                {
                    _shouldRender = true;
                    _isPreload = false;
                    return;
                }

                var token = _cts.Token;

                var userId = _userState.Model.Id;
                var userRoleId = _userState.Model.RoleId;

                var request = new AddOrUpdateAnnouncementRequest
                {
                    Id = _announcement.Id,
                    Title = _announcement.Title,
                    StartDate = _announcement.StartDate,
                    EndDate = _announcement.EndDate,
                    IsActive = _announcement.IsActive,
                    AnnouncementMessages = _announcementMessages.Where(m => !string.IsNullOrEmpty(m.Value)).Select(s => new AnnouncementMessage { LanguageCode = s.Key, Message = s.Value }).ToList(),
                    ClientCodes = _selectedClientCodes,
                    UserId = userId,
                    UserRoleId = userRoleId
                };

                if (_isEdit)
                {
                    var isInternalUser = _userState.Model.Claims.Any(c => c.Type.Contains("Admin.") && c.Value != "0") && _userState.Model.Claims.Any(c => c.Type.Contains("SBC.") && c.Value != "0");

                    if (isInternalUser == false && _announcement.UserRoleId != _userState.Model.RoleId)
                    {
                        var toRemoveClientCodes = _announcement.Clients.Select(s => s.ClientCode).ToList();
                        if (toRemoveClientCodes != null && toRemoveClientCodes.Any())
                        {
                            request.ToRemoveClientCodes.AddRange(toRemoveClientCodes);
                            _announcement.UserRoleId = userRoleId;
                            _isEdit = false;
                        }
                    }

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
                    _alerts.Add(new AnnouncementsAlertModel
                    {
                        AnnouncementAlertType = AnnouncementAlertType.GeneralError,
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
            if (selectedItems.Length > 0)
            {
                _selectedClientCodes = selectedItems.Split(",").ToList();
            }
            else
            {
                _selectedClientCodes.Clear();
            }
            _shouldRender = true;
        }

        public bool ShowNoTitleAlert() => _alerts.Any(a => a.AnnouncementAlertType == AnnouncementAlertType.NoTitle) && string.IsNullOrEmpty(_announcement.Title);
        public bool ShowNoMessageAlert() => _alerts.Any(a => a.AnnouncementAlertType == AnnouncementAlertType.NoMessage) && !_announcementMessages.Any(m => !string.IsNullOrEmpty(m.Value));
        public bool ShowInvalidStartDateAlert() => _alerts.Any(a => a.AnnouncementAlertType == AnnouncementAlertType.InvalidStartDate) && _announcement.StartDate > _announcement.EndDate;
        public bool ShowNoClientAlert() => _alerts.Any(a => a.AnnouncementAlertType == AnnouncementAlertType.NoClient) && !_selectedClientCodes.Any();
        public bool ShowGeneralErrorAlert() => _alerts.Any(a => a.AnnouncementAlertType == AnnouncementAlertType.GeneralError);
        public string GetDateRangeClass() => ShowInvalidStartDateAlert() ? "has-error" : "";

        public async Task HandleBlur()
        {
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
