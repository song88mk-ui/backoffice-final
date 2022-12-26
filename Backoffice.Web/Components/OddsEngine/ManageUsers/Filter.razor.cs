using Backoffice.Shared.Models;

namespace Backoffice.Web.Components.OddsEngine.ManageUsers
{
    public partial class Filter : IDisposable
    {
        #region Properties

        public SearchFormModel SearchForm { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SearchForm = new SearchFormModel();
        }

        public async Task OnInputSearchEvent()
        {
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
