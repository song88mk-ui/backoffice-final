using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class Breadcrumb : IDisposable
    {
        #region Properties

        [Parameter] public List<BreadcrumbModel> Breadcrumbs { get; set; }

        [Parameter] public string Region { get; set; }
      
        #endregion

        #region Methods

        public void Dispose()
        {
        }

        #endregion
    }
}
