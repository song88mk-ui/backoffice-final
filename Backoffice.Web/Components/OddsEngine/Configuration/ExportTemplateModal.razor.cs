using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ExportTemplateModal : IDisposable
    {
        #region Properties
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        #endregion

        private string _filename = "";
        private string _errorMessage = "";

        #region Methods
        protected override void OnInitialized() => _filename = ModalParameters.TryGet<string>("ConfigTemplateName");
        public async Task HandleExportClick()
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
                ConfigTemplateId = ModalParameters.TryGet<long>("ConfigTemplateId").ToString(),
                Filename = Path.GetFileNameWithoutExtension(_filename),
                AccessToken = _userState.AccessToken ?? string.Empty
            };

            // Task - Migrate to api call
            //await Task.Run(() => MessageBroker.Publish(JsonSerializer.Serialize(payload), "ExportOeConfigTemplate", $"r.bo.util.export", Guid.NewGuid().ToString()));

            _modalService.Cancel();
        }

        public void Dispose()
        {

        }
        #endregion
    }
}
