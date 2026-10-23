using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class ToastComponent2 : IDisposable
    {
        [Inject] private AlertToastService? _toasterService { get; set; }
        private bool _isLoad;

        private void ToastChanged(object? sender, EventArgs e)
        {
            _isLoad = false;
            this.InvokeAsync(this.StateHasChanged);
        }

        private void HandleLoadToasEvent(object? sender, EventArgs e)
        {
            _isLoad = true;
            this.InvokeAsync(this.StateHasChanged);
        }

        protected override void OnInitialized()
        {
            _toasterService.ToasterChanged += ToastChanged;
            _toasterService.LoadToastChanged += HandleLoadToasEvent;
        }

        public void Dispose()
        {
            _toasterService.ToasterChanged -= ToastChanged;
            _toasterService.LoadToastChanged -= HandleLoadToasEvent;
        }
    }
}
