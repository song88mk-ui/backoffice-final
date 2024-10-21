using Serilog;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Backoffice.Web.Services
{
    public class AlertToastService : IDisposable
    {
        public event EventHandler? ToasterChanged;
        public bool HasMessage => _message != null;

        private string? _message;
        private int secsToLive = 3;
        private CancellationTokenSource? _cancellationTokenSource;

        public string GetToast() => _message ?? string.Empty;
        public async Task SetToast(string message)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            _message = message;

            this.ToasterChanged?.Invoke(this, EventArgs.Empty);

            var token = _cancellationTokenSource.Token;
            await Task.Delay(secsToLive * 1000, token);

            if (token.IsCancellationRequested)
                return;

            ClearToast();
        }

        public void ClearToast()
        {
            _message = null;

            this.ToasterChanged?.Invoke(this, EventArgs.Empty);
        }
        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
