using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Models
{
    public class ModalOptions
    {
        public string Position { get; set; }

        public string Style { get; set; }
        public string Tooltip { get; set; }

        public bool? HideHeader { get; set; }

        public bool? DisableBackgroundCancel { get; set; }

        public bool? HideCloseButton { get; set; }

        public bool? HasHeaderBackButton { get; set; }
        public object? CallbackObject { get; set; }
        public ElementReference Element { get; set; }
    }
}
