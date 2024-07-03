using Backoffice.Web.Models;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Components;
using System.Diagnostics;

namespace Backoffice.Web.Shared
{
    public partial class PageHeader : IDisposable
    {
        [Parameter] public PageHeaderModel PageHeaderModel { get; set; }

        private string _title = "";
        private string _iconClass = "";

        //private int counter;

        protected override void OnParametersSet()
        {
            if (PageHeaderModel != null)
            {
                if (PageHeaderModel.Title != _title)
                    _title = PageHeaderModel.Title;
                if (PageHeaderModel.IconClass != _iconClass)
                    _iconClass = PageHeaderModel.IconClass;
            }

            //Debug.WriteLine(counter++);
        }

        public void Dispose()
        {
            PageHeaderModel = null;
        }
    }
}