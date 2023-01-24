using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.ManualBetSettlement
{
    public partial class AddReferenceModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {

        }
        public async Task OnModalResetClickEvent()
        {

        }

        public async Task OnModalSettleClickEvent()
        {

        }
        public void Dispose()
        {
            
        }
        #endregion
    }
}
