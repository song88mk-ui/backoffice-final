namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MarketGroups : IDisposable
    {
        #region Properties

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Market Groups";
            IconClass = string.Empty;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
