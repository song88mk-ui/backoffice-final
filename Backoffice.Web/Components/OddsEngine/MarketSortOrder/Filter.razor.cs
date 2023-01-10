namespace Backoffice.Web.Components.OddsEngine.MarketSortOrder
{
    public partial class Filter : IDisposable
    {
        #region Properties

        public bool IsUpdate { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            IsUpdate = false;
        }

        public async Task OnUpdateClickEvent()
        {
            IsUpdate = true;
        }

        public async Task OnCancelClickEvent()
        {
            IsUpdate = false;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
