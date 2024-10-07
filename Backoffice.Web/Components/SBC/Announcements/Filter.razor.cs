using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.SBC.Announcements
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }

        [CascadingParameter(Name = "UserClientCodes")] public List<string>? UserClientCodes { get; set; }

        [Parameter] public EventCallback<AnnouncementsFilterModel> OnInitCallback { get; set; }
        [Parameter] public EventCallback<AnnouncementsFilterModel> OnFilterCallback { get; set; }

        #endregion

        #region Fields

        private bool _init;
        private bool _shouldRender = true;

        private SearchFormModel _searchForm;
        private AnnouncementsFilterModel _filterModel;
        private List<SelectListItemWithCheckboxModel<string>> _clients;
        private List<SelectListItemWithCheckboxModel<string>> _languageCodes;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _init = true;
            _filterModel = new AnnouncementsFilterModel();

            _searchForm = new SearchFormModel()
            {
                SearchText = string.Empty,
                IsReset = false
            };

            _languageCodes = Enum.GetValues<LanguageCode>().Select(s => new SelectListItemWithCheckboxModel<string>
            {
                Value = s.ToString(),
                Name = s.ToString(),
                IsChecked = true
            }).ToList();
            _filterModel.LanguageCode = _languageCodes.FirstOrDefault()?.Value ?? string.Empty;

            var clients = (await BackofficeService.GetClients()).Result;

            if (clients != null && clients.Clients.Any())
            {
                clients.Clients = UserClientCodes != null && UserClientCodes.Any() ? clients.Clients.Where(c => UserClientCodes.Contains(c.ClientCode.ToUpper())).ToList() : new List<Client>();

                _clients = clients.Clients.OrderBy(x => x.ClientName).Select(s => new SelectListItemWithCheckboxModel<string>
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
            }

            _filterModel.Search = string.Empty;
            await OnInitCallback.InvokeAsync(_filterModel);

        }

        protected override async Task OnParametersSetAsync()
        {
            _shouldRender = _init && _clients != null;

            if(_clients != null)
                _init = false;
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

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
