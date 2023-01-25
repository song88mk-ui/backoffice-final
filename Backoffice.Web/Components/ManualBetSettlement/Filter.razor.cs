using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.ManualBetSettlement
{
    public partial class Filter : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        public bool ShowFilter { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ShowFilter = false;
        }

        public async Task OnAddReferenceClickEvent()
        {
            ModalParameters parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 80rem");

            ModalOptions options = new ModalOptions();

            ModalService.Open<AddReferenceModal>("Add Reference ID", parameters, options);
        }

        public async Task OnFilterClickEvent()
        {
            ShowFilter = !ShowFilter;            
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
