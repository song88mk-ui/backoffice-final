namespace Backoffice.Web.Pages.Administration
{
    public partial class Maintenance : IDisposable
    {
        #region Properties

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Maintenance";
            IconClass = "ico ico-maintenance";
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
