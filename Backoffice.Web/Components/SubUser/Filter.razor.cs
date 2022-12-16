using Backoffice.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.SubUser
{
    public partial class Filter : IDisposable
    {
        #region Properties
        [Parameter] public EventCallback OnInitCallback { get; set; }
        [Parameter] public EventCallback OnCreateSubUserCallback { get; set; }
        public SearchFormModel SearchForm { get; set; }


        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SearchForm = new SearchFormModel();

            await OnInitCallback.InvokeAsync();
        }

        public async Task OnInputSearchEvent()
        {
            await OnInitCallback.InvokeAsync();
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
