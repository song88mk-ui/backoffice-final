using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Toaster = Backoffice.Web.Services.Toast;

namespace Backoffice.Web.Components.Common
{
    public partial class ToastComponent : ComponentBase, IDisposable
    {
        [Inject] private ToastService? _toasterService { get; set; }

        private ToastService toasterService => _toasterService!;
        private void ToastChanged(object? sender, EventArgs e)
                                    => this.InvokeAsync(this.StateHasChanged); 
        protected override void OnInitialized()
        {
            this.toasterService.ToasterChanged += ToastChanged;
            this.toasterService.ToasterTimerElapsed += ToastChanged;
        }
        private void ClearToast(Toaster toast)
                                    => toasterService.ClearToast(toast);

        public void Dispose()
        {
            this.toasterService.ToasterChanged -= ToastChanged;
            this.toasterService.ToasterTimerElapsed -= ToastChanged;
        }
    }
}
