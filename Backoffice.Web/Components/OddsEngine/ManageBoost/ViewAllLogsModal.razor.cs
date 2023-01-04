using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Page = Backoffice.Shared.Enums.Page;

namespace Backoffice.Web.Components.OddsEngine.ManageBoost
{
    public partial class ViewAllLogsModal : IDisposable
    {
        #region Properties
        [Inject] IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        public string DialogStyle { get; set; }
        public Page CurrentPage { get; set; }
        #endregion

        protected override async Task OnInitializedAsync()
        {
            DialogStyle = ModalParameters.TryGet<string>("DialogStyle");
            CurrentPage = ModalParameters.TryGet<Page>("SelectedPage");
        }
        #region Methods
        public async Task OnCancelModalClick()
        {
            ModalService.Cancel();
        }
        public void Dispose()
        {
            
        }
        #endregion
    }
}
