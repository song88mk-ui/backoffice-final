namespace Backoffice.Web.Shared.OddsEngine
{
    public partial class HeaderNavMenu : IDisposable
    {
        #region Properties

        public bool ShowPriceBoostMenu { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ShowPriceBoostMenu = false;
        }

        public async Task OnPriceBoostClickEvent()
        {
            if (!ShowPriceBoostMenu) 
                ShowPriceBoostMenu = true;
            else if (ShowPriceBoostMenu) 
                ShowPriceBoostMenu = false;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
