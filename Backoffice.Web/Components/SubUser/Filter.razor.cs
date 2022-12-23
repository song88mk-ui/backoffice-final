using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.SubUser
{
    public partial class Filter : IDisposable
    {
        #region Properties
        [Parameter] public EventCallback<SubUserCallbackModel> OnInitCallback { get; set; }
        [Parameter] public EventCallback OnCreateSubUserCallback { get; set; }
        public SearchFormModel SearchForm { get; set; }


        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SearchForm = new SearchFormModel();

            await OnInitCallback.InvokeAsync(new SubUserCallbackModel { IsFirstTime = true, SearchText = string.Empty });
        }

        public async Task OnInputSearchEvent()
        {
            await OnInitCallback.InvokeAsync(new SubUserCallbackModel { IsFirstTime = false, SearchText = SearchForm.SearchText });
        }

        public async Task OnCreateSubUserClickEvent()
        {
            await OnCreateSubUserCallback.InvokeAsync();
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
