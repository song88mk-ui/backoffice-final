using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.SBC.Announcements;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class Announcements : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }

        public MetaData MetaData { get; set; }

        #endregion

        #region Fields

        private decimal _opacity;

        private bool _init = true;
        private bool _shouldRender;
        private bool _isPreLoader = true;

        private LanguageCode _languageCode;
        private AnnouncementsModel _model;
        private List<AlertModel> _alerts;
        private CancellationTokenSource _cts;
        private PageHeaderModel _pageHeaderModel;
        private AnnouncementsFilterModel _filterModel;

        #endregion

        #region Methods     

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            _pageHeaderModel = new()
            {
                Title = "Announcements",
                //IconClass = "ico ico-announcements"
            };

            _alerts = new List<AlertModel>();
            _opacity = 0m;

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 10000,
                TotalPages = 100,
            };

            await AuthenticateAccess.InvokeAsync();
        }

        protected override Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameters();

            if (_shouldRender)
                _languageCode = _translationState.ActiveLanguageCode;

            _init = false;
            return base.OnParametersSetAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            _shouldRender = false;
        }

        protected override bool ShouldRender() => _shouldRender;

        public async Task OnInitEvent(AnnouncementsFilterModel model)
        {
            _filterModel = model;
            await GetContent(model);

            _shouldRender = true;
        }

        public async Task OnFilterEvent(AnnouncementsFilterModel model)
        {
            if (model.IsAddAnnouncement)
            {
                _alerts.Clear();

                var parameters = new ModalParameters();

                parameters.Add("ModalClass", "modal-center modal-show");
                parameters.Add("ModalDialogClass", "modal-l");
                parameters.Add("Announcement", new Models.Announcements
                {
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow,
                    LanguageCode = LanguageCode.EN,
                    IsActive = true
                });
                parameters.Add("Clients", model.Clients);
                parameters.Add("IsEdit", false);
                parameters.Add("ModalHeaderClass", "announcements-modal-header");

                var options = new ModalOptions();
                options.HideCloseButton = true;
                ModalService.Open<CreateOrUpdateModal>("Announcements", parameters, options);

                model.IsAddAnnouncement = false;
            }
            else
            {
                MetaData = new MetaData
                {
                    CurrentPage = 1,
                    PageSize = 25,
                    TotalCount = 0,
                    TotalPages = 1,
                };

                await GetContent(model);
            }

            _shouldRender = true;
        }

        public async Task OnEditClickEvent(Models.Announcements model)
        {
            _alerts.Clear();

            if (!string.IsNullOrEmpty(model.DisabledClass))
            {
                _shouldRender = true;
                return;
            }

            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-l");
            parameters.Add("Announcement", model);
            parameters.Add("Clients", _filterModel.Clients);
            parameters.Add("IsEdit", true);

            var options = new ModalOptions();
            options.HideCloseButton = true;
            ModalService.Open<CreateOrUpdateModal>("Edit Announcements", parameters, options);

            _shouldRender = true;
        }

        public async Task OnDeleteClickEvent(Models.Announcements model)
        {
            _alerts.Clear();

            if (!string.IsNullOrEmpty(model.DisabledClass))
            {
                _shouldRender = true;
                return;
            }

            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show announcements-delete");
            parameters.Add("ModalHeaderClass", "announcements-delete-header");
            parameters.Add("ModalDialogClass", "modal-l");
            parameters.Add("Announcement", model);
            parameters.Add("ModalIcon", 
                $"<div class=\"announcements-delete-container\" style=\"padding: 9px 10px; border-radius: 50%; background-color: #FEF3F2;\"><i class=\"fa fa-trash\" style=\"padding: 6px 10px; border-radius: 50%; background-color: #FEE4E2; color: #D92D20; font-size: 24px;\"/></div>");

            var options = new ModalOptions();
            options.HideCloseButton = false;
            ModalService.Open<DeleteAnnouncementModal>("", parameters, options);

            _shouldRender = true;
        }

        public async Task OnSaveClickEvent(object item)
        {
            _opacity = _opacity == 1m ? 0.99m : 1m;

            var message = "Announcement successfully updated";
            if (item.GetType() == typeof(AnnouncementResponse))
            {
                message = "Announcement successfully created";

                var result = (AnnouncementResponse)item;

                MetaData.TotalCount = MetaData.TotalCount + result.Announcements.Count;
                MetaData.TotalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(MetaData.TotalCount) / Convert.ToDecimal(MetaData.PageSize)));
                MetaData.CurrentPage = MetaData.TotalPages;

                await GetContent(_filterModel);
            }
            else if (item.GetType() == typeof(int))
            {
                message = "Announcement successfully deleted";

                var announcement = _model.Announcements.FirstOrDefault(a => a.Id == (int)item);
                if (announcement != null)
                {
                    _model.Announcements.Remove(announcement);

                    await GetContent(_filterModel);
                }
            }

            _alerts.Clear();
            _alerts.Add(new AlertModel
            {
                Status = AlertStatus.Success,
                Message = message,
                Opacity = _opacity
            });
            
            _shouldRender = true;
        }

        public async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;

            await GetContent(_filterModel);
            _shouldRender = true;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        private bool ValidateParameters()
        {
            return _init || _languageCode != _translationState.ActiveLanguageCode;
        }

        private async Task GetContent(AnnouncementsFilterModel model)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            if (model.ClientCodes == null)
                return;

            if (!_isPreLoader)
            {
                _isPreLoader = true;
                _shouldRender = true;
            }

            var token = _cts.Token;
            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), model.LanguageCode, true);
            var response = await BackofficeService.GetAnnouncements(new AnnouncementRequest
            {
                ClientCodes = model.ClientCodes,
                LanguageCode = languageCode,
                SearchText = model.Search,
                Page = MetaData.CurrentPage,
                PageSize = MetaData.PageSize
            }, token);

            if (token.IsCancellationRequested)
                return;

            MetaData.TotalCount = response.Result.TotalCount;
            MetaData.TotalPages = response.Result.TotalPage;

            _model = new AnnouncementsModel
            {
                Announcements = response.Result.Announcements.Select((s, index) => new Models.Announcements
                {
                    Id = s.Id,
                    EndDate = s.EndDate,
                    StartDate = s.StartDate,
                    IsActive = s.IsActive,
                    Message = s.Message,
                    Title = s.Title,
                    LanguageCode = s.LanguageCode,
                    Clients = s.Clients.Where(c => _filterModel.Clients.Select(s => s.Value).Contains(c.ClientCode)).ToList(),
                    Languages = s.Languages,
                    UserRoleId = s.UserRoleId,
                    UserId = s.UserId,
                    Row = index + ((MetaData.CurrentPage - 1) * MetaData.PageSize) + 1,
                }).ToList()
            };

            SetItemAction();

            _isPreLoader = false;
        }

        private void SetItemAction()
        {
            var isInternalUser = _userState.Model.Claims.Any(c => c.Type.Contains("Admin.") && c.Value != "0") && _userState.Model.Claims.Any(c => c.Type.Contains("SBC.") && c.Value != "0");
            foreach (var item in _model.Announcements)
            {
                item.EditAction = _ => OnEditClickEvent(item);
                item.DeleteAction = _ => OnDeleteClickEvent(item);
                item.DisabledClass = isInternalUser == false && !item.Clients.Any(c => _userState.Model.SportsbookClients.Any(sbc => sbc.ClientCode.Equals(c.ClientCode, StringComparison.OrdinalIgnoreCase))) ? "disabled-action" : "";
            }
        }

        private string GetClientsConcat(List<AnnouncementResponse.ClientInfo> clients)
        {
            if (!clients.Any()) return "";

            var tempConcat = String.Join(", ", clients.Select(c => c.ClientCode).Take(3).ToList());

            return clients.Count > 3 ? tempConcat + "..." : tempConcat;
        }

        private int GetHiddenClientsCount(List<AnnouncementResponse.ClientInfo> clients)
        {
            if (clients.Count <= 3) return 0;

            return clients.Count - 3;
        }

        private bool ShowClientsCount(List<AnnouncementResponse.ClientInfo> clients)
        {
            return clients.Count > 3;
        }

        private string GetStatusLabel(bool isActive)
        {
            return isActive ? _translationState.GetValue("Active").ToUpper() : _translationState.GetValue("Inactive").ToUpper();
        }

        private string GetLanguagesConcat(List<AnnouncementMessage> langList)
        {
            if (!langList.Any()) return string.Empty;

            return String.Join(", ", langList.Select(s => s.LanguageCode).ToList());
        }

        #endregion
    }
}