using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;
using Page = Backoffice.Shared.Enums.Page;

namespace Backoffice.Web.Components.OddsEngine.ManageBoost
{
    public partial class ViewAllLogsFilter : IDisposable
    {
        #region Properties
        [Parameter] public Page SelectedPage { get; set; }
        [Parameter] public EventCallback<PriceBoostLogFilterModel> OnFilterChangedEventCallback { get; set; }
        [Parameter] public PriceBoostLogFilterModel FilterModel { get; set; }
        [Parameter] public EventCallback<PriceBoostLogFilterModel> FilterModelChanged { get; set; }
        public Dictionary<Page, string> Pages { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Pages = new Dictionary<Page, string>() { { Page.OE_PriceBoost_Manage, "Manage Boost" }, { Page.OE_PriceBoost_Settings, "Boosted Setting" } };

            FilterModel = new PriceBoostLogFilterModel()
            {
                Clients = new List<KeyValuePair<string, string>>(),
                Tab = SelectedPage,
                ClientCode = "ALL",
                Sports = new SportsLookupResponse(),
                SportId = 0,
                Segments = new SegmentLookupResponse(),
                SegmentId = 0,
                Leagues = new LeagueLookupResponse(),
                LeagueId = 0,
                SearchText = string.Empty,
                DateFrom = DateTime.Now.AddDays(-7).Date,
                DateTo = DateTime.Now.Date
            };
        }
        #endregion

        #region Events
        public async void OnPageSelectionChangedEvent(ChangeEventArgs e)
        {
            FilterModel.Tab = SelectedPage = (Page)Enum.Parse(typeof(Page), e.Value.ToString());

            await OnFilterChangedEventCallback.InvokeAsync(FilterModel);
            await InvokeAsync(StateHasChanged);
        }
        public async Task OnDateRangeChangedEvent(DateRange range)
        {
            FilterModel.DateFrom = DateTime.Parse(range.Start.ToString()).Date.Add(new TimeSpan(0, 0, 0));
            FilterModel.DateTo = DateTime.Parse(range.End.ToString()).Date.Add(new TimeSpan(23, 59, 59));

            await OnFilterChangedEventCallback.InvokeAsync(FilterModel);
            await InvokeAsync(StateHasChanged);
        }
        public async Task OnSubmitClickEvent()
        {
            await OnFilterChangedEventCallback.InvokeAsync(FilterModel);
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnClientChangedEvent(ChangeEventArgs e)
        {
            FilterModel.ClientCode = e.Value.ToString();

            await OnFilterChangedEventCallback.InvokeAsync(FilterModel);
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnSportChangedEvent(ChangeEventArgs e)
        {
            FilterModel.SportId = int.Parse(e.Value.ToString());

            await OnFilterChangedEventCallback.InvokeAsync(FilterModel);
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnSegmentChangedEvent(ChangeEventArgs e)
        {
            FilterModel.SegmentId = int.Parse(e.Value.ToString());

            await OnFilterChangedEventCallback.InvokeAsync(FilterModel);
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnLeagueChangedEvent(ChangeEventArgs e)
        {
            FilterModel.LeagueId = int.Parse(e.Value.ToString());

            await OnFilterChangedEventCallback.InvokeAsync(FilterModel);
            await InvokeAsync(StateHasChanged);
        }

        #endregion

        public void Dispose()
        {
        }
    }
}
