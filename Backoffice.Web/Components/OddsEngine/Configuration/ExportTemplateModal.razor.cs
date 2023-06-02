using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ExportTemplateModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }                
        #endregion


        #region Methods
        public async Task OnExportClickEvent()
        {   
            ModalService.Cancel();
        }

        public async Task OnInputFileChangedEvent(InputFileChangeEventArgs e)
        {

        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
