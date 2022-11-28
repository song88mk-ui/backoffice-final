using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.TopLeagues
{
    public partial class EditModal : IDisposable
    {
        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        #endregion


        #region Methods
        protected override async Task OnInitializedAsync()
        {
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
