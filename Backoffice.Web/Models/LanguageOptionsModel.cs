using Backoffice.Shared.Enums;
using Microsoft.AspNetCore.Components.Web;

namespace Backoffice.Web.Models
{
    public class LanguageOptionsModel
    {
        public LanguageOptionsModel()
        {
            Languages = new List<LanguageOptions>();
        }
        public List<LanguageOptions> Languages { get; set; }
    }

    public class LanguageOptions
    {
        public LanguageCode Language { get; set; }
        public Action<MouseEventArgs> ClickAction { get; set; }
    }
}
