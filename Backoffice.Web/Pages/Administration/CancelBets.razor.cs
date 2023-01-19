namespace Backoffice.Web.Pages.Administration
{
    public partial class CancelBets
    {
        #region Properties

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Cancel Bets";
            IconClass = "ico ico-cancel-bets";
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
