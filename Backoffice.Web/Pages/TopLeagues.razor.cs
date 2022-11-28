namespace Backoffice.Web.Pages
{
    public partial class TopLeagues : IDisposable
    {
        public string PageTitle { get; set; }

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Top Leagues";
        }

        public void Dispose()
        {
        }
    }
}
