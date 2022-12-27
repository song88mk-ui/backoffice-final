using Backoffice.Shared.Models;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MessageSettlement : IDisposable
    {
        #region Properties

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        public MetaData MetaData { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Message Settlement";
            IconClass = string.Empty;

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
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
