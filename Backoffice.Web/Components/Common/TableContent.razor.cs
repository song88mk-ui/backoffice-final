using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class TableContent
    {
        #region Properties

        [Parameter]
        public string Class { get; set; }
        [Parameter]
        public string THeadClass { get; set; }
        [Parameter]
        public RenderFragment TableHeaderContent { get; set; }
        [Parameter]
        public RenderFragment TableBodyContent { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            if (string.IsNullOrEmpty(Class))
                Class = "table table-bordered table-hover no-margin";
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
