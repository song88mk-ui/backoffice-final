using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Components.Common;
using Microsoft.AspNetCore.Components;
using Backoffice.Web.Services;

namespace Backoffice.Web.Components.OddsEngine.Monitoring
{
    public partial class SwitchAllFixturesModal : IDisposable
    {

        [Inject] public IModalService ModalService { get; set; }

        public void OnCloseClickEvent()
        {
            ModalService.Cancel();
        }
        public void Dispose()
        {
        }
    }
}
