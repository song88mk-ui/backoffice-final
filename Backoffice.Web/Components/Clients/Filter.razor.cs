using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Clients
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Parameter] public EventCallback<ClientListCallbackModel> OnInitCallback { get; set; }

        public SearchFormModel SearchForm { get; set; }
        public int SortBy { get; set; }
        public List<SelectListItemModel<int>> SelectSortByList { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SearchForm = new SearchFormModel();
            SelectSortByList = new List<SelectListItemModel<int>>();

            SelectSortByList.Add(new SelectListItemModel<int>
            {
                Name = "Sort by Business Name (Ascending)",
                Value = (int)ClientSorting.SortByBusinessNameAsc
            });

            SelectSortByList.Add(new SelectListItemModel<int>
            {
                Name = "Sort by Business Name (Descending)",
                Value = (int)ClientSorting.SortByBusinessNameDesc
            });

            SelectSortByList.Add(new SelectListItemModel<int>
            {
                Name = "Sort by Create Date (Ascending)",
                Value = (int)ClientSorting.SortByCreatedDateAsc
            });

            SelectSortByList.Add(new SelectListItemModel<int>
            {
                Name = "Sort by Create Date (Descending)",
                Value = (int)ClientSorting.SortByCreatedDateDesc
            });

            SortBy = (int)ClientSorting.SortByBusinessNameAsc;

            await OnInitCallback.InvokeAsync(new ClientListCallbackModel
            {
                SearchText = SearchForm.SearchText,
                SortBy = SortBy
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

        public void Dispose()
        {
        }

        #endregion
    }
}
