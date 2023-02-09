using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Administration.ManageUser
{
    public partial class Filter
    {
        #region Properties

        [Parameter] public EventCallback OnCreateUserCallback { get; set; }
        [Parameter] public EventCallback OnInitCallback { get; set; }
        [Parameter] public ManageUserFilterModel FilterModel { get; set; }
        [Parameter] public EventCallback<ManageUserFilterModel> FilterModelChanged { get; set; }
        public SearchFormModel SearchForm { get; set; }
        public List<SelectListItemModel<int>> SelectSortByList { get; set; }
        public int SelectedSortBy { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            FilterModel = new ManageUserFilterModel();
            SearchForm = new SearchFormModel();
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

            await FilterModelChanged.InvokeAsync(FilterModel);
            await OnInitCallback.InvokeAsync();
        }
        public async Task OnSortByChangedEvent(ChangeEventArgs e)
        {
            var sortBy = Convert.ToInt32(e.Value);
            FilterModel.UserSorting = (UserSorting)sortBy;

            await FilterModelChanged.InvokeAsync(FilterModel);
            await OnInitCallback.InvokeAsync();
        }
        public async Task OnInputSearchEvent()
        {
            await FilterModelChanged.InvokeAsync(FilterModel);
            await OnInitCallback.InvokeAsync();
        }

        public async Task OnCreateUserClickEvent()
        {
            await OnCreateUserCallback.InvokeAsync();
        }

        #endregion

    }
}
