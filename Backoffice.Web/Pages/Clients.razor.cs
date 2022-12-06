using Backoffice.Shared.Models;

namespace Backoffice.Web.Pages
{
    public partial class Clients : IDisposable
    {
        #region Properties

        public MetaData MetaData { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 100,
                TotalCount = 0,
                TotalPages = 1,
            };

            PageTitle = "Manage Clients";
            IconClass = "ico-clients";
        }

        public async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
