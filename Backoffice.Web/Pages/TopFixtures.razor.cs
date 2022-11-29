using Backoffice.Shared.Models;

namespace Backoffice.Web.Pages
{
    public partial class TopFixtures : IDisposable
    {
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public MetaData MetaData { get; set; } = new MetaData();


        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Top Fixtures";
            IconClass = "ico-top-fixtures";

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 10000,
                TotalPages = 100,
            };
        }

        protected async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;
            //await GetContents();
        }

        public void Dispose()
        {
        }
    }
}
