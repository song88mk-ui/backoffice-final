using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class CustomModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Parameter] public EventCallback<object> OnSaveClickCallback { get; set; }
        [Parameter] public EventCallback<object> OnCancelClickCallback { get; set; }
        private RenderFragment ModalContent { get; set; }
        private ModalParameters ModalParameters { get; set; }
        private string ToolTip { get; set; }
        private bool HideCloseButton { get; set; }
        private bool HasHeaderBackButton { get; set; }
        private object CallBackObject { get; set; }
        #endregion

        #region Fields

        public string _modalDisplay = "none;";
        public string _modalClass = "";
        public string _modalDialogClass = "";
        public bool _showBackdrop = false;
        public string _modalTitle = "";
        public string _modalDialogStyle = "";

        #endregion

        #region ctor
        protected override void OnInitialized()
        {
            ((ModalService)ModalService).OnOpen += Open;
            ((ModalService)ModalService).CloseModal += Close;
        }
        #endregion

        #region Methods
        public void Open(string title, RenderFragment content, ModalParameters parameters, ModalOptions options)
        {
            _modalTitle = title;
            ModalContent = content;
            ModalParameters = parameters;

            var style = parameters.TryGet<string>("DialogStyle");
            var modalClass = parameters.TryGet<string>("ModalClass");
            var modalDialogClass = parameters.TryGet<string>("ModalDialogClass");

            _modalDisplay = "block";
            _modalClass = string.IsNullOrEmpty(modalClass) ? "show" : modalClass;
            _modalDialogClass = string.IsNullOrEmpty(modalDialogClass) ? "" : modalDialogClass;
            _modalDialogStyle = !string.IsNullOrEmpty(style) ? style : "";

            ToolTip = options.Tooltip;
            HideCloseButton = options.HideCloseButton == null ? false : (bool)options.HideCloseButton;
            HasHeaderBackButton = options.HasHeaderBackButton == null ? false : (bool)options.HasHeaderBackButton;
            CallBackObject = options.CallbackObject;

            _showBackdrop = true;

            StateHasChanged();
        }

        public void Close()
        {
            _modalDisplay = "none";
            _modalClass = "";
            _modalDialogStyle = "";
            _showBackdrop = false;
            ModalContent = null;

            StateHasChanged();
        }

        public async Task OnBack()
        {
            if (HasHeaderBackButton)
                await OnCancelClickCallback.InvokeAsync(CallBackObject);
        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
