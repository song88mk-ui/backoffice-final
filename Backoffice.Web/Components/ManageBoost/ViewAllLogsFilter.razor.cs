using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.ManageBoost
{
    public partial class ViewAllLogsFilter : IDisposable
    {
        #region Properties
        [Inject] public ITradingService TradingService { get; set; }
        [Parameter] public SportsbookClientPermission SelectedPage { get; set; }
        [Parameter] public EventCallback<PriceBoostLogFilterModel> OnFilterChangedEventCallback { get; set; }
        [Parameter] public PriceBoostLogFilterModel FilterModel { get; set; } = new PriceBoostLogFilterModel();
        [Parameter] public EventCallback<PriceBoostLogFilterModel> FilterModelChanged { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        public Dictionary<SportsbookClientPermission, string> Pages { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Pages = new Dictionary<SportsbookClientPermission, string>() { { SportsbookClientPermission.SBC_OE_PriceBoost_Manage, "Manage Boost" }, { SportsbookClientPermission.SBC_OE_PriceBoost_Settings, "Boosted Setting" } };

            var client = await GetClients();
            FilterModel = new PriceBoostLogFilterModel()
            {
                Clients = client,
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
            await OnFilterChangedEventCallback.InvokeAsync(FilterModel);
        }
        #endregion

        #region Events
        public async void OnPageSelectionChangedEvent(ChangeEventArgs e)
        {
            FilterModel.Tab = SelectedPage = (SportsbookClientPermission)Enum.Parse(typeof(SportsbookClientPermission), e.Value.ToString());

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

        #region Utilities
        private async Task<List<KeyValuePair<string, string>>> GetClients()
        {
            var response = await TradingService.GetClientList();

            List<KeyValuePair<string, string>> Clients = new List<KeyValuePair<string, string>>();

            if (response != null)
            {
                foreach (var client in response.Result)
                {
                    if (UserModel.ClientCodes.Contains(client.ClientCode, StringComparer.OrdinalIgnoreCase))
                    {
                        KeyValuePair<string, string> Client = new KeyValuePair<string, string>(client.ClientCode, client.ClientName);

                        Clients.Add(Client);
                    }
                }
            }

            return Clients;
        }
        #endregion
    }
}
