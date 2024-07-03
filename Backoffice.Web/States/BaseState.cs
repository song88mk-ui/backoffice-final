using Microsoft.AspNetCore.Components;
using System.ComponentModel;
using System.Formats.Tar;
using System.Runtime.CompilerServices;

namespace Backoffice.Web.States
{
    public abstract class BaseState<T> : INotifyPropertyChanged
    {
        private T _model;

        protected T Model
        {
            get { return _model; }
            set
            {
                _model = value;
                OnPropertyChanged();
            }
        }
        protected BaseState(T initialValue)
        {
            _model = initialValue;
        }
        public event PropertyChangedEventHandler? PropertyChanged = null!;
        protected void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
