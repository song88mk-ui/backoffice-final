namespace Backoffice.Web.Pages.Administration
{
    public partial class ResendSettlement : IDisposable
    {
        #region Properties

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Resend Settlement";
            IconClass = "ico ico-resend-settlement";
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
