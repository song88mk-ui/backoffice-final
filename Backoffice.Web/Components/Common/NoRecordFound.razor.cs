using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class NoRecordFound
    {
        [Parameter] public string Message { get; set; } = "";
    }
}
