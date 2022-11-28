using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Shared
{
    public partial class PageHeader : IDisposable
    {
        [Parameter] public string PageTitle { get; set; }

        public void Dispose()
        {
        }
    }
}
