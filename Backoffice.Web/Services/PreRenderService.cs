using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Services
{
    public class PreRenderService : IPreRenderService
    {
        [Inject] IJSRuntime? JSRuntime { get; set; }

        public bool IsPreRendering { get => _isPreRendering; }
        public HttpContext? HttpContext { get; }

        private bool _isPreRendering = false;

        public PreRenderService()
        {
            if (JSRuntime is null || !(JSRuntime is IJSInProcessRuntime))
            {
                throw new ApplicationException("Please add an HttpContextAccessor to detect PreRendering");
            }
        }

        public PreRenderService(IHttpContextAccessor httpContextAccessor)
        {
            HttpContext = httpContextAccessor?.HttpContext;

            if (HttpContext?.Response.HasStarted ?? false)
                _isPreRendering = false;
            else
                _isPreRendering = true;
        }
    }
}
