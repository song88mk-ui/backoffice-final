namespace Backoffice.Web.Pages
{
    public partial class ParlayBonus : IDisposable
    {
        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Parlay Bonus";
            IconClass = "ico-parlay-bonus";
        }

        public void Dispose()
        {
        }
    }
}
