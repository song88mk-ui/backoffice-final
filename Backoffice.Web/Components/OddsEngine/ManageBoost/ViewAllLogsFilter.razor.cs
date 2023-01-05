using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Page = Backoffice.Shared.Enums.Page;

namespace Backoffice.Web.Components.OddsEngine.ManageBoost
{
    public partial class ViewAllLogsFilter : IDisposable
    {
        #region Properties
        [Parameter] public Page SelectedPage { get; set; }

        public Dictionary<Page, string> Pages { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Pages = new Dictionary<Page, string>() { { Page.OE_PriceBoost_Manage, "Manage Boost"}, { Page.OE_PriceBoost_Settings, "Boosted Setting"} };

            StartDate = DateTime.Now.AddDays(-7).Date;
            EndDate = DateTime.Now.Date;
        }
        #endregion

        #region Events
        public async void OnPageSelectionChangedEvent(ChangeEventArgs e)
        {
            SelectedPage = (Page)Enum.Parse(typeof(Page), e.Value.ToString());
            await InvokeAsync(StateHasChanged);
        }
        #endregion

        public void Dispose()
        {
        }
    }
}
