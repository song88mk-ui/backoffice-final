namespace Backoffice.Web.Services
{
    public class SaveOptionService : IDisposable
    {
        public EventHandler? OnSaved;
        public EventHandler? OnCancel;

        private bool _isOpen;

        public void Open() => _isOpen = true;
        public void Close() => _isOpen = false;
        private void HandleSaveClick() => OnSaved?.Invoke(this, EventArgs.Empty);
        private void HandleCancelClick()
        {
            _isOpen = false;
            OnCancel?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {

        }
    }
}
