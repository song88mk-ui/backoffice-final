namespace Backoffice.Web.Components.ManualBetSettlement
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
            if (ShowFilter)
                ShowFilter = false;
            else
                ShowFilter = true;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
