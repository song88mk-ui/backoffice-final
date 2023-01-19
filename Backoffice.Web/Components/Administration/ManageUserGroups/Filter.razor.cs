using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Administration.ManageUserGroups
{
    public partial class Filter : IDisposable
    {
        #region Properties
        [Parameter] public EventCallback<ClientListCallbackModel> OnInitCallback { get; set; }
        [Parameter] public EventCallback<bool> OnCreateUserGroupCallback { get; set; }
        protected SearchFormModel SearchForm { get; set; }

        protected int SortBy { get; set; }
        protected List<SelectListItemModel<int>> SelectSortByList { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SearchForm = new SearchFormModel();
            SelectSortByList = new List<SelectListItemModel<int>>();

            SelectSortByList.Add(new SelectListItemModel<int>
            {
                Name = "Sort by User Group Name (Ascending)",
                Value = (int)UserGroupSorting.SortByUserGroupNameAsc
            });

            SelectSortByList.Add(new SelectListItemModel<int>
            {
                Name = "Sort by User Group Name (Descending)",
                Value = (int)UserGroupSorting.SortByUserGroupNameDesc
            });

            SelectSortByList.Add(new SelectListItemModel<int>
            {
                Name = "Sort by Create Date (Ascending)",
                Value = (int)UserGroupSorting.SortByCreatedDateAsc
            });

            SelectSortByList.Add(new SelectListItemModel<int>
            {
                Name = "Sort by Create Date (Descending)",
                Value = (int)UserGroupSorting.SortByCreatedDateDesc
            });

        }
        public async Task OnInputSearchEvent()
        {
            await OnInitCallback.InvokeAsync(new ClientListCallbackModel
            {
                SearchText = SearchForm.SearchText,
                SortBy = SortBy
            });
        }

        public async Task OnSortByChangedEvent(ChangeEventArgs e)
        {
            SortBy = Convert.ToInt32(e.Value);

            await OnInitCallback.InvokeAsync(new ClientListCallbackModel
            {
                SearchText = SearchForm.SearchText,
                SortBy = SortBy
            });
        }

        public async Task OnCreateUserGroupCallbackEvent()
        {
            await OnCreateUserGroupCallback.InvokeAsync();
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
