namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class Monitoring : IDisposable
    {
        #region Properties

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "American Football - USA - NCAA, Regular Season";
            IconClass = "iconfon iconfont-american-football";
        }

        public void Dispose()
        {
        }

        #endregion

    }
}
