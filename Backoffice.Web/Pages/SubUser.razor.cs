using Backoffice.Shared.Models;

namespace Backoffice.Web.Pages
{
    public partial class SubUser : IDisposable
    {
        #region Properties

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        public MetaData MetaData { get; set; }

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

            PageTitle = "Sub Users";
            IconClass = "ico-sub-user";
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
