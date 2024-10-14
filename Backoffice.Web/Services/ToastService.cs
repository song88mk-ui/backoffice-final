using System.Runtime.CompilerServices;
using System.Timers;
using Timer = System.Timers.Timer;
using Serilog;

namespace Backoffice.Web.Services
{
    public record Toast
    {
        public Guid Id = Guid.NewGuid();
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public bool IsMessageMarkup { get; set; } = false;
        public DateTimeOffset TimeToBurn { get; init; }
        public readonly DateTimeOffset Posted = DateTimeOffset.Now;
        public bool IsBurnt => TimeToBurn < DateTimeOffset.Now;
        private TimeSpan elapsedTime => Posted - DateTimeOffset.Now;
        private static int _secToLive = 20;

        public string ElapsedTimeText =>
                        elapsedTime.Seconds > 60
                        ? $"posted {-elapsedTime.Minutes} mins ago"
                        : $"posted {-elapsedTime.Seconds} secs ago";
        public static Toast NewToast(string title, string message)
                             => NewToast(title, message, _secToLive, false);
        public static Toast NewToast(string title, string message, bool IsMessageMarkup)
                             => NewToast(title, message, _secToLive, IsMessageMarkup);
        public static Toast NewToast(string title, string message, int secsToLive)
                             => NewToast(title, message, secsToLive, false);
        public static Toast NewToast(string title, string message, int secsToLive, bool IsMessageMarkup)
                             => new Toast
                             {
                                 Id = Guid.NewGuid(),
                                 Title = title,
                                 Message = message,
                                 IsMessageMarkup = IsMessageMarkup,
                                 TimeToBurn = DateTimeOffset.Now.AddSeconds(secsToLive)
                             };
    }
    public class ToastService : IDisposable
    {
        private List<Toast> _toastList = new List<Toast>();
        private Timer _timer = new Timer();
        public event EventHandler? ToasterChanged;
        public event EventHandler? ToasterTimerElapsed;
        public bool HasToasts => _toastList.Count > 0;
        public ToastService()
        {
            _timer.Interval = 1000;
            _timer.AutoReset = true;
            _timer.Elapsed += this.TimerElapsed;
            _timer.Start();
        }
        private bool ClearBurntToast()
        {
            try
            {
                var toastsToDelete = _toastList.Where(item => item.IsBurnt).ToList();
                if (toastsToDelete is not null && toastsToDelete.Count > 0)
                {
                    toastsToDelete.ForEach(toast => _toastList.Remove(toast));
                    this.ToasterChanged?.Invoke(this, EventArgs.Empty);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

            }
            return false;
        }
        private void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            this.ClearBurntToast();
            this.ToasterTimerElapsed?.Invoke(this, EventArgs.Empty);
        }
        public List<Toast> GetToasts()
        {
            ClearBurntToast();
            return _toastList;
        }

        public void AddToast(Toast toast)
        {
            _toastList.Insert(0, toast);
            //_toastList = _toastList.Prepend(toast).ToList();

            if (!this.ClearBurntToast())
                this.ToasterChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ClearToast(Toast toast)
        {
            if (_toastList.Contains(toast))
            {
                //_toastList.Remove(_toastList.FirstOrDefault(x => x.Id == toast.Id));

                if (!this.ClearBurntToast())
                    this.ToasterChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public void Dispose()
        {
            //Console.WriteLine("- {0} was disposed!", this.GetType().Name);

            _timer.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
