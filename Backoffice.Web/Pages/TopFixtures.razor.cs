namespace Backoffice.Web.Pages
{
    public partial class TopFixtures : IDisposable
    {
        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Top Fixtures";
            IconClass = "ico-top-fixtures";
        }

        public void Dispose()
        {
        }
    }
}
