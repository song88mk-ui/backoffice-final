using Backoffice.Web.Models;
using System.Drawing;
using Microsoft.AspNetCore.Components;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.OddsEngine.Mapping;
using Backoffice.Web.Services;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class Mapped
    {
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        public async Task OnEditMappingClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-lg");
            var options = new ModalOptions();

            ModalService.Open<EditMappingModal>("Edit Mapping Modal", parameters, options);
        }

        public async Task OnSaveClickEvent()
        {
        }
    }
}
