using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class MinimalistModal
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

        private string _modalDisplay = "none;";
        private string _modalClass = "";
        private string _modalDialogClass = "";
        private bool _showBackdrop = false;
        private string _modalDialogStyle = "";
        private string _modalHeaderClass = "";

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
            ModalContent = content;
            ModalParameters = parameters;

            var style = parameters.TryGet<string>("DialogStyle");
            var modalClass = parameters.TryGet<string>("ModalClass");
            var modalDialogClass = parameters.TryGet<string>("ModalDialogClass");
            var modalHeaderClass = parameters.TryGet<string>("ModalHeaderClass");
            // modal-lg d-flex justify-content-center align-items-center h-100
            _modalDisplay = "block";
            _modalClass = string.IsNullOrEmpty(modalClass) ? "show" : modalClass;
            _modalDialogClass = !string.IsNullOrEmpty(modalDialogClass) ? modalDialogClass : "";
            _modalDialogStyle = !string.IsNullOrEmpty(style) ? style : "";
            _modalHeaderClass = !string.IsNullOrEmpty(modalHeaderClass) ? modalHeaderClass : "";

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
