namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MonitoringDetails : IDisposable
    {
        #region Properties

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "American Football - USA - NCAA, Regular Season";
            IconClass = "fa fa-trophy";
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
