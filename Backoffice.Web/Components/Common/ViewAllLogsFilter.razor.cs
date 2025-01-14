using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common;
public partial class ViewAllLogsFilter : IDisposable
{
    #region Properties
    [Parameter] public SportsbookClientPermission SelectedPage { get; set; }
    [Parameter] public EventCallback<PriceBoostLogFilterModel> OnFilterChangedEventCallback { get; set; }
    [Parameter] public PriceBoostLogFilterModel FilterModel { get; set; } = new PriceBoostLogFilterModel();
    [Parameter] public EventCallback<PriceBoostLogFilterModel> FilterModelChanged { get; set; }
    public Dictionary<SportsbookClientPermission, string> Pages { get; set; }
    #endregion

    #region Methods
    protected override async Task OnInitializedAsync()
    {
        Pages = new Dictionary<SportsbookClientPermission, string>() { { SportsbookClientPermission.SBC_OE_PriceBoost_Manage, "Manage Boost" }, { SportsbookClientPermission.SBC_OE_PriceBoost_Settings, "Boosted Settings" } };

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
            DateFrom = DateTimeHelper.Now(_userState.TimeZoneOffset).AddDays(-7).Date,
            DateTo = DateTimeHelper.Now(_userState.TimeZoneOffset).Date
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
        var clientDateRange = DateTimeHelper.ConvertToClientTimeZone(range, _userState.TimeZoneOffset);
        FilterModel.DateFrom = clientDateRange.Start.DateTime;
        FilterModel.DateTo = clientDateRange.End.DateTime;

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
        var response = await _tradingService.GetClientList();

        List<KeyValuePair<string, string>> Clients = new List<KeyValuePair<string, string>>();

        if (response != null)
        {
            foreach (var client in response.Result)
            {
                if (_userState.ClientCodes.Contains(client.ClientCode, StringComparer.OrdinalIgnoreCase))
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
