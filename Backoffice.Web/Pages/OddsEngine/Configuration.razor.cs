namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class Configuration : IDisposable
    {
        #region Properties
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Config";
            IconClass = "";
        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
