namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ManageUsers : IDisposable
    {
        #region Properties

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Manage Users";
            IconClass = string.Empty;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
