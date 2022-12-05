using Backoffice.Web.Components.TopLeagues;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages
{
    public partial class TopLeagues : IDisposable
    {
        [Inject] public IModalService ModalService { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public string ClientName { get; set; } = "";

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Top Leagues";
            IconClass = "ico-top-leagues";
        }

        public async Task OnEdit()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");

            var options = new ModalOptions();
            ModalService.Open<EditModal>($"Set Top Leagues of {ClientName}", parameters, options);
        }

        public async Task OnCopy()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");

            var options = new ModalOptions();
            ModalService.Open<CopyModal>($"Copy {ClientName} Top League Settings To", parameters, options);
        }

        public void Dispose()
        {
        }
    }
}
