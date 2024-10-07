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

        [Parameter] public EventCallback<AnnouncementsFilterModel> OnInitCallback { get; set; }
        [Parameter] public EventCallback<AnnouncementsFilterModel> OnFilterCallback { get; set; }
        [Parameter] public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }


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

            if (Clients != null && Clients.Any())
            {
                _clients = Clients;

                var selectedClients = _clients.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

                _filterModel.ClientCodes = string.Join(",", selectedClients);
            }
            else
            {
                _clients = new List<SelectListItemWithCheckboxModel<string>>();
            }

            _filterModel.Search = string.Empty;
            await OnInitCallback.InvokeAsync(_filterModel);
        }

        //private int ctr = 0;
        //protected override async Task OnParametersSetAsync()
        //{
        //    ctr++;
        //}

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
