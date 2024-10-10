using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Services
{
    public interface IModalService
    {
        event Action<ModalResult> OnClose;

        void Open<T>(string title, ModalParameters parameters) where T : ComponentBase;
        void Open<T>(ModalParameters parameters, ModalOptions options) where T : ComponentBase;

        void Open<T>(string title, ModalParameters parameters = null, ModalOptions options = null) where T : ComponentBase;

        void Open(Type contentComponent, string title, ModalParameters parameters, ModalOptions options);

        void Close(ModalResult modalResult);
        void Cancel();
    }
}
