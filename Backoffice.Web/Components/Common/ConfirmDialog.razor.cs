using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    // Rendered inline by its host rather than through ModalService, which holds a single modal:
    // opening a confirmation there would replace (and lose) the modal asking for it.
    public partial class ConfirmDialog
    {
        [Parameter] public bool Visible { get; set; }
        [Parameter] public string Message { get; set; } = string.Empty;
        [Parameter] public string ConfirmText { get; set; } = "Confirm";
        [Parameter] public string CancelText { get; set; } = "Cancel";
        [Parameter] public EventCallback OnConfirm { get; set; }
        [Parameter] public EventCallback OnCancel { get; set; }
    }
}
