using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;

namespace Backoffice.Web.Components.ManageUser
{
    public partial class Filter
    {
        #region Properties

        public SearchFormModel SearchForm { get; set; }
        public List<SelectListItemModel<int>> SelectSortByList { get; set; }
        public int SelectedSortBy { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SearchForm= new SearchFormModel();
            SelectSortByList = new List<SelectListItemModel<int>>();

            SelectSortByList.Add(new SelectListItemModel<int>
            {
                Name = "Sort by Username (Ascending)",
                Value = (int)UserSorting.SortByUsernameAsc
            });

            SelectSortByList.Add(new SelectListItemModel<int>
            {
                Name = "Sort by Username (Descending)",
                Value = (int)UserSorting.SortByUsernameDesc
            });

            SelectSortByList.Add(new SelectListItemModel<int>
            {
                Name = "Sort by Create Date (Ascending)",
                Value = (int)UserSorting.SortByCreateDateAsc
            });

            SelectSortByList.Add(new SelectListItemModel<int>
            {
                Name = "Sort by Create Date (Descending)",
                Value = (int)UserSorting.SortByCreateDateDesc
            });

            SelectedSortBy = (int)UserSorting.SortByUsernameAsc;
        }

        public async Task OnInputSearchEvent()
        {
        }

        #endregion

    }
}
