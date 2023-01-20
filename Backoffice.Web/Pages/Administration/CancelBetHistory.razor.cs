namespace Backoffice.Web.Pages.Administration
{
    public partial class CancelBetHistory : IDisposable
    {
        #region Properties

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Cancel Bet History";
            IconClass = "ico ico-cancel-bet-history";
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
