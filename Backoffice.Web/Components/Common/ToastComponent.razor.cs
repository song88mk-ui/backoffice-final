using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Toaster = Backoffice.Web.Services.Toast;

namespace Backoffice.Web.Components.Common
{
    public partial class ToastComponent : ComponentBase, IDisposable
    {
        [Inject] private ToastService? _toasterService { get; set; }
        private void ToastChanged(object? sender, EventArgs e)
                                    => this.InvokeAsync(this.StateHasChanged); 
        protected override void OnInitialized()
        {
            _toasterService.ToasterChanged += ToastChanged;
            _toasterService.ToasterTimerElapsed += ToastChanged;
        }
        private void ClearToast(Toaster toast)
                                    => _toasterService.ClearToast(toast);

        public void Dispose()
        {
            _toasterService.ToasterChanged -= ToastChanged;
            _toasterService.ToasterTimerElapsed -= ToastChanged;
        }
    }
}
