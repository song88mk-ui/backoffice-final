using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class ConfirmationModal : IDisposable
    {
        [Inject] public IBackofficeService BackofficeService { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }

        private int _userAction; // 0 - cancel, 1 - save update
        private bool IsUpdating;
        private bool IsPreLoader;
        protected override async Task OnInitializedAsync()
        {
            var test = ModalParameters?.TryGet<string>("UserAction");

            _userAction = ModalParameters?.TryGet<string>("UserAction").ToUpper() switch
            {
                "CANCELING" => 0,
                "LEAVING" => 1,
                _ => -1
            };
        }

        public void Dispose()
        {
        }
    }
}
