using Backoffice.Shared.Models;

namespace Backoffice.Web.Pages.Administration
{
    public partial class ManualBetSettlement : IDisposable
    {
        #region Properties

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public MetaData MetaData { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Manual Bet Settlement";
            IconClass = "ico ico-manual-bet-settlement";

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 100,
                TotalCount = 0,
                TotalPages = 1,
            };
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
