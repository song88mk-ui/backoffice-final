using Backoffice.Shared.Enums;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class Alert : IDisposable
    {
        [Parameter] public AlertStatus AlertStatus { get; set; }
        [Parameter] public string Message { get; set; }

        public void Dispose()
        {
        }
    }
}
