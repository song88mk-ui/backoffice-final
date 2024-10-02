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


        private SearchFormModel _searchForm;
        private AnnouncementsFilterModel _filterModel;
        private List<SelectListItemWithCheckboxModel<string>> _clients;
        private List<SelectListItemWithCheckboxModel<string>> _languageCodes;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
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

                _filterModel.ClientIds = string.Join(",", selectedClients);
            }
            else
            {
                _clients = new List<SelectListItemWithCheckboxModel<string>>();
            }

            _filterModel.Search = string.Empty;
            await OnInitCallback.InvokeAsync(_filterModel);
        }

        public async Task OnClientCheckboxClickEvent(string selectedItems)
        {
            _filterModel.ClientIds = selectedItems;

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
