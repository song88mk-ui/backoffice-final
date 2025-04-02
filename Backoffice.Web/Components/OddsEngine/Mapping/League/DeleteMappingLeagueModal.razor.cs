using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Mapping;
using Backoffice.Web.Services;
using Backoffice.Web.Components.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;


namespace Backoffice.Web.Components.OddsEngine.Mapping.League
{
    public partial class DeleteMappingLeagueModal: IDisposable
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter] private CustomModal Modal { get; set; }
        public void Dispose()
        {
        }
    }

}