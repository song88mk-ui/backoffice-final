using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.Utilities;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.SBC.Announcements
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }

        [Parameter] public EventCallback<AnnouncementsFilterModel> OnInitCallback { get; set; }
        [Parameter] public EventCallback<AnnouncementsFilterModel> OnFilterCallback { get; set; }

        #endregion

        #region Fields

        private bool _init = true;
        private bool _shouldRender;

        private LanguageCode _languageCode;
        private SearchFormModel _searchForm;
        private AnnouncementsFilterModel _filterModel;
        private List<SelectListItemWithCheckboxModel<string>> _clients;
        private List<SelectListItemWithCheckboxModel<string>> _languageCodes;
        private List<Client> _clientListReference;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {//
            _filterModel = new AnnouncementsFilterModel();

            _searchForm = new SearchFormModel()
            {
                SearchText = string.Empty,
                IsReset = false
            };

            _languageCodes = Enum.GetValues<LanguageCode>().Select(s => new SelectListItemWithCheckboxModel<string>
            {
                Value = s.ToString(),
                Name = Utility.GetLanguageCodeFullLabel(s),
                IsChecked = true
            }).ToList();
            _filterModel.LanguageCode = _languageCodes.FirstOrDefault()?.Value ?? string.Empty;
            _filterModel.Search = string.Empty;

        }

        protected override async Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameters();

            _init = false;

            if (_shouldRender)
            {
                _languageCode = _translationState.ActiveLanguageCode;

                if (_userState.SportsbookClients != null && _clientListReference != _userState.SportsbookClients)
                    _clientListReference = _userState.SportsbookClients;

                if (_clientListReference != null && _clientListReference.Any())
                {
                    _clients = _clientListReference.OrderBy(x => x.ClientName).Select(s => new SelectListItemWithCheckboxModel<string>
                    {
                        Value = s.ClientCode,
                        Name = s.ClientName,
                        IsChecked = true
                    }).ToList();

                    var selectedClients = _clients.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

                    _filterModel.Clients = _clients;
                    _filterModel.ClientCodes = string.Join(",", selectedClients);
                }
                else
                {
                    _clients = new List<SelectListItemWithCheckboxModel<string>>();
                    _filterModel.ClientCodes = null;
                }

                await OnInitCallback.InvokeAsync(_filterModel);

            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            _shouldRender = false;
        }

        protected override bool ShouldRender() => _shouldRender;

        public async Task OnClientCheckboxClickEvent(string selectedItems)
        {
            _filterModel.ClientCodes = selectedItems;

            await OnFilterCallback.InvokeAsync(_filterModel);
        }

        public async Task OnLanguageCheckboxClickEvent(string selectedItem)
        {
            _filterModel.LanguageCode = selectedItem;

            await OnFilterCallback.InvokeAsync(_filterModel);
        }

        public async Task OnInputSearchEvent()
        {
            _filterModel.Search = _searchForm.SearchText;

            await OnFilterCallback.InvokeAsync(_filterModel);
        }

        public async Task OnCreateAnnouncementClickEvent()
        {
            _filterModel.IsAddAnnouncement = true;

            await OnFilterCallback.InvokeAsync(_filterModel);
        }

        private bool ValidateParameters()
        {
            return _init || _clientListReference != _userState.SportsbookClients || _languageCode != _translationState.ActiveLanguageCode;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
