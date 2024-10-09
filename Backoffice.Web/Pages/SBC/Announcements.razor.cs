using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.SBC.Announcements;
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
            _pageHeaderModel = new()
            {
                Title = "Announcements",
                IconClass = "ico ico-announcements"
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
                _languageCode = TranslationState.ActiveLanguageCode;

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
                    StartDate = DateTime.Now.ToLocalTime(),
                    EndDate = DateTime.Now.ToLocalTime(),
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

            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-l");
            parameters.Add("Announcement", model);

            var options = new ModalOptions();
            options.HideCloseButton = true;
            ModalService.Open<DeleteAnnouncementModal>("Delete Announcement", parameters, options);

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

                MetaData.TotalCount = _model.Announcements.Count + result.Announcements.Count;
                MetaData.TotalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(MetaData.TotalCount) / Convert.ToDecimal(MetaData.PageSize)));

                _model.Announcements.AddRange(result.Announcements.Select((s, index) =>
                    new Models.Announcements
                    {
                        Id = s.Id,
                        IsActive = s.IsActive,
                        Clients = s.Clients,
                        Message = s.Message,
                        Title = s.Title,
                        LanguageCode = s.LanguageCode,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        Row = index + _model.Announcements.Count + ((MetaData.CurrentPage - 1) * MetaData.PageSize) + 1,
                    }).ToList());

                SetItemAction();
            }
            else if (item.GetType() == typeof(int))
            {
                message = "Announcement successfully deleted";

                var announcement = _model.Announcements.FirstOrDefault(a => a.Id == (int)item);
                if (announcement != null)
                    _model.Announcements.Remove(announcement);


                MetaData.TotalCount = _model.Announcements.Count;
                MetaData.TotalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(MetaData.TotalCount) / Convert.ToDecimal(MetaData.PageSize)));

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
            return _init || _languageCode != TranslationState.ActiveLanguageCode;
        }

        private async Task GetContent(AnnouncementsFilterModel model)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            if (!_isPreLoader)
                _isPreLoader = true;

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
                    Row = index + ((MetaData.CurrentPage - 1) * MetaData.PageSize) + 1,
                }).ToList()
            };

            SetItemAction();

            _isPreLoader = false;
        }

        private void SetItemAction()
        {
            foreach (var item in _model.Announcements)
            {
                item.EditAction = _ => OnEditClickEvent(item);
                item.DeleteAction = _ => OnDeleteClickEvent(item);
            }
        }

        #endregion
    }
}