namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class SportSettings : IDisposable
    {
        #region Properties

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Sport Settings";
            IconClass = string.Empty;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
