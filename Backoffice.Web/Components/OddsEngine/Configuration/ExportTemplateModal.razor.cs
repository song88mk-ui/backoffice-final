using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.JSInterop;
using System.Text.Json;

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
            //var payload = new OnGetOeConfigTemplateMessage()
            //{
            //    ConnectionId = "",
            //    UserId = "",
            //    ConfigTemplateId = "",
            //    Filename = "",
            //    AccessToken = ""
            //};

            //MessageBroker.Publish(JsonSerializer.Serialize(payload), "ExportOeConfigTemplate", $"r.bo.util.export", Guid.NewGuid().ToString(), "HK");

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
