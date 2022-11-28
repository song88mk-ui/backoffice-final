namespace Backoffice.Web.Pages
{
    public partial class TopFixtures : IDisposable
    {
        public string PageTitle { get; set; }

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Top Fixtures";
        }

        public void Dispose()
        {
        }
    }
}
