namespace Backoffice.Web.Components.OddsEngine.MessageSettlement
{
    public partial class Filter : IDisposable
    {
        #region Properties

        public bool ShowFilter { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ShowFilter = false;
        }

        public async Task OnFilterClickEvent()
        {
            if (!ShowFilter)
                ShowFilter = true;
            else if (ShowFilter)
                ShowFilter = false;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
