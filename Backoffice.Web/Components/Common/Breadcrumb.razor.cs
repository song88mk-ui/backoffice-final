using Backoffice.Web.Models;
using Backoffice.Web.Shared.OddsEngine;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class Breadcrumb : IDisposable
    {
        #region Properties

        [Parameter] public List<BreadcrumbModel> Breadcrumbs { get; set; }
        [Parameter] public string Region { get; set; }

        [CascadingParameter(Name = "HeaderNavMenuReference")] public HeaderNavMenu HeaderNavMenuReference { get; set; }

        #endregion

        public void Dispose()
        {
        }
    }
}
