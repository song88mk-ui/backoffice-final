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
        private bool HideHeader { get; set; }
        private bool HasHeaderBackButton { get; set; }
        private object CallBackObject { get; set; }
        #endregion

        #region Fields

        private string _modalDisplay = "none;";
        private string _modalClass = "";
        private string _modalDialogClass = "";
        private bool _showBackdrop = false;
        private string _modalTitle = "";
        private string _modalDialogStyle = "";
        private string _modalIcon = "";
        private string _titleClass = "";
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
            _modalTitle = title;
            ModalContent = content;
            ModalParameters = parameters;

            var style = parameters.TryGet<string>("DialogStyle");
            var modalClass = parameters.TryGet<string>("ModalClass");
            var modalDialogClass = parameters.TryGet<string>("ModalDialogClass");
            var modalHeaderClass = parameters.TryGet<string>("ModalHeaderClass");
            var modalIcon = parameters.TryGet<string>("ModalIcon");
            var modalTitleClass = parameters.TryGet<string>("TitleClass");

            _modalDisplay = "block";
            _modalClass = string.IsNullOrEmpty(modalClass) ? "show" : modalClass;
            _modalDialogClass = string.IsNullOrEmpty(modalDialogClass) ? "" : modalDialogClass;
            _modalDialogStyle = !string.IsNullOrEmpty(style) ? style : "";
            _modalHeaderClass = !string.IsNullOrEmpty(modalHeaderClass) ? modalHeaderClass : "";
            _modalIcon = !string.IsNullOrEmpty(modalIcon) ? modalIcon : "";
            _titleClass = !string.IsNullOrEmpty(modalTitleClass) ? modalTitleClass : "";

            ToolTip = options.Tooltip;
            HideHeader = options.HideHeader == null ? false : (bool)options.HideHeader;
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
