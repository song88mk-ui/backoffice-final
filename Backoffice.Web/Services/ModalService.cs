using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Web.Models;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Services
{
    public class ModalService : IModalService, IDisposable
    {
        private readonly TranslationState _translationState;
        

        public event Action<ModalResult> OnClose;
        internal event Action CloseModal;
        internal event Action<string, RenderFragment, ModalParameters, ModalOptions> OnOpen;
        private Type _modalType;
        public ModalService(TranslationState translationState)
        {
            _translationState = translationState;
        }
        public void Open<T>(string title, ModalParameters parameters) where T : ComponentBase
        {
            Open<T>(title, parameters, new ModalOptions());
        }

        public void Open<T>(string title, ModalParameters parameters = null, ModalOptions options = null) where T : ComponentBase
        {
            Open(typeof(T), title, parameters, options);
        }

        public async void Open(Type contentComponent, string title, ModalParameters parameters, ModalOptions options)
        {
            if (!typeof(ComponentBase).IsAssignableFrom(contentComponent))
            {
                throw new ArgumentException("Must be a Blazor component");
            }

            var content = new RenderFragment(s => { s.OpenComponent(1, contentComponent); s.CloseComponent(); });
            _modalType = contentComponent;

             OnOpen?.Invoke(_translationState.GetValue(title), content, parameters, options);
        }

        public void Cancel()
        {
            CloseModal?.Invoke();
            OnClose?.Invoke(ModalResult.Cancel(_modalType));
        }

        public void Close(ModalResult modalResult)
        {
            modalResult.ModalType = _modalType;
            CloseModal?.Invoke();
            OnClose?.Invoke(modalResult);
        }

        public void Dispose()
        {
            Console.WriteLine("- {0} was disposed!", this.GetType().Name);
        }
    }
}
