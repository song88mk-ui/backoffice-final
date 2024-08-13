using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Shared.OddsEngine
{
    public partial class OEPageHeader : IDisposable
    {
        [Parameter] public PageHeaderModel PageHeaderModel { get; set; }

        private string _title = "";
        private string _iconClass = "";
        private bool _showLanguageDropdown;

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