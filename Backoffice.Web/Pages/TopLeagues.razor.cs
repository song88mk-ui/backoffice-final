namespace Backoffice.Web.Pages
{
    public partial class TopLeagues : IDisposable
    {
        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Top Leagues";
            IconClass = "ico-top-leagues";
        }

        public void Dispose()
        {
        }
    }
}
