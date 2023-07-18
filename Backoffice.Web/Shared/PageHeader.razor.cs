using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Shared
{
    public partial class PageHeader : IDisposable
    {
        [Parameter] public PageHeaderModel PageHeaderModel { get; set; }

        private string _title = "";
        private string _iconClass = "";
        protected override void OnParametersSet()
        {
            if (PageHeaderModel != null)
            {
                if (PageHeaderModel.Title != _title)
                    _title = PageHeaderModel.Title;
                if (PageHeaderModel.IconClass != _iconClass)
                    _iconClass = PageHeaderModel.IconClass;
            }
        }
        public void Dispose()
        {
            PageHeaderModel = null;
        }
    }
}
