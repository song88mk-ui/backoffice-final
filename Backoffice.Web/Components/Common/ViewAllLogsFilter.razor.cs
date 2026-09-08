using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Backoffice.Web.Pages.SBC;
using Backoffice.Web.Services;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Backoffice.Web.Components.Common;

public partial class ViewAllLogsFilter : IDisposable
{
    #region Properties
    [Parameter] public SportsbookClientPermission SelectedPage { get; set; }
    [Parameter] public EventCallback<PriceBoostLogFilterModel> OnFilterChangedEventCallback { get; set; }
    [Parameter] public PriceBoostLogFilterModel FilterModel { get; set; } = new PriceBoostLogFilterModel();
    [Parameter] public EventCallback<PriceBoostLogFilterModel> FilterModelChanged { get; set; }
    public Dictionary<SportsbookClientPermission, string> Pages { get; set; }

    private bool _shouldRender = true;
    private List<SelectListItemModel<int>> _sports = new List<SelectListItemModel<int>>();
    private List<SelectListItemModel<int>> _segments = new List<SelectListItemModel<int>>();
    private List<SelectListItemModel<int>> _leagues = new List<SelectListItemModel<int>>();
    private LanguageCode _languageCode;

    #endregion

    #region Methods
    protected override async Task OnInitializedAsync()
    {
        Pages = new Dictionary<SportsbookClientPermission, string>() { { SportsbookClientPermission.SBC_OE_PriceBoost_Manage, "Manage Boost" }, { SportsbookClientPermission.SBC_OE_PriceBoost_Settings, "Boosted Settings" } };

        var client = await GetClients();

        _sports = await LoadSports();

        FilterModel = new PriceBoostLogFilterModel()
        {
            Clients = client,
            Tab = SelectedPage,
            ClientCode = "ALL",
            SportId = _sports?.FirstOrDefault()?.Value ?? 0,
            SegmentId = 0,
            LeagueId = 0,
            SearchText = string.Empty,
            DateFrom = DateTimeHelper.Now(_userState.TimeZoneOffset).AddDays(-7).Date,
            DateTo = DateTimeHelper.Now(_userState.TimeZoneOffset).Date
        };
        await OnFilterChangedEventCallback.InvokeAsync(FilterModel);
    }

    protected override async Task OnParametersSetAsync()
    {
        _shouldRender = ValidateParameters();

        if (_shouldRender)
        {
            _languageCode = _translationState.ActiveLanguageCode;

            _sports = await LoadSports();
        }

        await Task.CompletedTask;
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
        FilterModel.SegmentId = 0;
        FilterModel.LeagueId = 0;

        _leagues = new List<SelectListItemModel<int>>();

        if (FilterModel.SportId == 0)
        {
            _segments = new List<SelectListItemModel<int>>();
        }
        else
        {
            _segments = await LoadSegments(FilterModel.SportId);
        }

        await OnFilterChangedEventCallback.InvokeAsync(FilterModel);
    }

    public async Task OnSegmentChangedEvent(ChangeEventArgs e)
    {
        FilterModel.SegmentId = int.Parse(e.Value.ToString());
        FilterModel.LeagueId = 0;

        if (FilterModel.SegmentId == 0)
        {
            _leagues = new List<SelectListItemModel<int>>();
        }
        else
        {
            _leagues = await LoadLeagues(FilterModel.SportId, FilterModel.SegmentId);
        }
        await OnFilterChangedEventCallback.InvokeAsync(FilterModel);
    }

    public async Task OnLeagueChangedEvent(ChangeEventArgs e)
    {
        FilterModel.LeagueId = int.Parse(e.Value.ToString());

        await OnFilterChangedEventCallback.InvokeAsync(FilterModel);
    }

    #endregion

    public void Dispose()
    {
    }

    #region Utilities

    private bool ValidateParameters()
    {
        return _languageCode != _translationState.ActiveLanguageCode;
    }

    private async Task<List<KeyValuePair<string, string>>> GetClients()
    {
        List<KeyValuePair<string, string>> Clients = new List<KeyValuePair<string, string>>();

        if (_userState.Model.TradingClients != null)
        {
            foreach (var client in _userState.Model.TradingClients)
            {
                KeyValuePair<string, string> Client = new KeyValuePair<string, string>(client.ClientCode, client.ClientName);

                Clients.Add(Client);
            }
        }

        return Clients;
    }

    private async Task<List<SelectListItemModel<int>>> LoadSports()
    {
        var response = await _backofficeService.GetSportLookup(_languageCode.ToString());

        var sportsLookup = response.Result;
        if (sportsLookup == null)
            return new();

        var result = sportsLookup.SportLookup.Select(s => new SelectListItemModel<int>()
        {
            Value = s.SportId,
            Name = s.SportName
        }).ToList();

        result.Insert(0, new()
        {
            Value = 0,
            Name = "All Sports"
        });

        return result;
    }

    private async Task<List<SelectListItemModel<int>>> LoadSegments(int sportId)
    {
        var response = await _backofficeService.GetSegmentLookup(sportId, _languageCode);

        var segmentsLookup = response.Result;
        if (segmentsLookup == null)
            return new();

        var result = segmentsLookup.SegmentLookup.Select(s => new SelectListItemModel<int>()
        {
            Value = s.SegmentId,
            Name = s.SegmentName
        }).ToList();

        result.Insert(0, new()
        {
            Value = 0,
            Name = "All Segments"
        });

        return result;
    }

    private async Task<List<SelectListItemModel<int>>> LoadLeagues(int sportId, int segmentId)
    {
        var response = await _backofficeService.GetLeagueLookup(sportId, segmentId, _languageCode);

        var leaguesLookup = response.Result;
        if (leaguesLookup == null)
            return new();

        var result = leaguesLookup.LeagueLookup.Select(s => new SelectListItemModel<int>()
        {
            Value = s.LeagueId,
            Name = s.LeagueName
        }).Take(100).ToList();

        result.Insert(0, new()
        {
            Value = 0,
            Name = "All Leagues"
        });

        return result;
    }
    #endregion
}
