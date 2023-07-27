using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.JSInterop;
using RabbitMQ.Client;
using System.Security.Permissions;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ExportTemplateModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] public IMessageBroker MessageBroker { get; set; }
        [Inject] public IHttpContextAccessor HttpContextAccessor { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        #endregion

        private string _filename = "";
        private string _errorMessage = "";

        #region Methods
        protected override void OnInitialized()
        {
            _filename = ModalParameters.TryGet<string>("ConfigTemplateName");
        }
        public async Task OnExportClickEvent()
        {
            _errorMessage = string.Empty;

            if (string.IsNullOrEmpty(_filename))
            {
                _errorMessage = "File name cannot be empty";
                return;
            }

            var payload = new OnGetOeConfigTemplateMessage()
            {
                ConnectionId = ModalParameters.TryGet<string>("ConnectionId"),
                UserId = ModalParameters.TryGet<string>("UserId"),
                ConfigTemplateId = ModalParameters.TryGet<string>("ConfigTemplateId"),
                Filename = Path.GetFileNameWithoutExtension(_filename),
                AccessToken = HttpContextAccessor.HttpContext?.Session.GetString("UserToken") ?? string.Empty
            };

            await Task.Run(() =>
            {
                MessageBroker.Publish(JsonSerializer.Serialize(payload), "ExportOeConfigTemplate", $"r.bo.util.export", Guid.NewGuid().ToString(), "HK");
            });

            ModalService.Cancel();
        }

        public void Dispose()
        {

        }
        #endregion
    }
}
