namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ManageUsergroups : IDisposable
    {
        #region Properties

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Manage User Group";
            IconClass = string.Empty;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
