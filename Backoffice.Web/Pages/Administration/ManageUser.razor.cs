namespace Backoffice.Web.Pages.Administration
{
    public partial class ManageUser : IDisposable
    {
        #region Properties

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Manage User";
            IconClass = "ico ico-manage-user";
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
