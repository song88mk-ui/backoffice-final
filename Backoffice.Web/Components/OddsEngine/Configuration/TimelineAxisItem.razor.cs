namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class TimelineAxisItem : IDisposable
    {
        #region Properties

        //test data only
        public List<string> TimelineList { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            TimelineList = new List<string>()
            {
                "240D","120D","60D","30D","15D","10D","9D","8D","7D","6D","5D",
                "4D","3D","2.5D","2D","36h","24h","18h","12h","6h","3h","2h","1h",
                "30m","15m"
            };
        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
