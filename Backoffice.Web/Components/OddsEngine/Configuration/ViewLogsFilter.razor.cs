using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ViewLogsFilter : IDisposable
    {
        #region Properties
        [Inject] private ITradingService TradingService { get; set; }

        [Parameter] public ConfigLogsFilterModel ConfigLogsFilterModel { get; set; }
        [Parameter] public EventCallback<ConfigLogsFilterModel> ConfigLogsFilterModelChanged { get; set; }
        [Parameter] public EventCallback GetContentCallback { get; set; }

        [CascadingParameter(Name = "UserClientCodes")] public List<string>? UserClientCodes { get; set; }
        private bool IsFilterButtonDisabled { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            IsFilterButtonDisabled = true;
            await Initialize();
        }

        public async Task Initialize()
        {
            var marketTemplateids = new List<string>();

            ConfigLogsFilterModel = new ConfigLogsFilterModel()
            {
                FixtureTypeLookUp = GetFixtureTypes(),
                Clients = await GetClients(),
                ClientId = Guid.Empty,
                Sports = await GetSports(),
                LeagueId = 0,
                MarketTemplateGroupId = 0,
                MarketTemplateIds = marketTemplateids,
                Scopes = GetScopes(),
                Scope = string.Empty,
                MarketId = string.Empty,
                FromDate = DateTime.Now,
                ToDate = DateTime.Now,
                Filter = string.Empty,
            };

            IsFilterButtonDisabled = false;
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnSubmitClickEvent()
        {
            await ConfigLogsFilterModelChanged.InvokeAsync(ConfigLogsFilterModel);
            await GetContentCallback.InvokeAsync();
        }

        public async Task OnFixtureTypeChangedEvent(ChangeEventArgs e)
        {
            ConfigLogsFilterModel.FixtureTypeId = int.Parse(e.Value.ToString());
            await ConfigLogsFilterModelChanged.InvokeAsync(ConfigLogsFilterModel);
            await GetContentCallback.InvokeAsync();
        }

        public async Task OnClientChangedEvent(ChangeEventArgs e)
        {
            ConfigLogsFilterModel.ClientId = Guid.Parse(e.Value.ToString());
            await ConfigLogsFilterModelChanged.InvokeAsync(ConfigLogsFilterModel);
            await GetContentCallback.InvokeAsync();
        }

        public async Task OnSportsChangedEvent(ChangeEventArgs e)
        {
            ConfigLogsFilterModel.SportId = int.Parse(e.Value.ToString());
            ConfigLogsFilterModel.Segments = await GetSegments(ConfigLogsFilterModel.SportId);
            ConfigLogsFilterModel.Leagues = await GetLeagues(ConfigLogsFilterModel.SportId, ConfigLogsFilterModel.SegmentId);
            ConfigLogsFilterModel.MarketTemplateGroups = await GetMarketGroups(ConfigLogsFilterModel.FixtureTypeId, ConfigLogsFilterModel.SportId);
            ConfigLogsFilterModel.Markets = await GetMarkets(ConfigLogsFilterModel.FixtureTypeId, ConfigLogsFilterModel.SportId, ConfigLogsFilterModel.MarketTemplateIds);
            await ConfigLogsFilterModelChanged.InvokeAsync(ConfigLogsFilterModel);
            await GetContentCallback.InvokeAsync();
        }

        public async Task OnSegmentChangedEvent(ChangeEventArgs e)
        {
            ConfigLogsFilterModel.SegmentId = int.Parse(e.Value.ToString());
            ConfigLogsFilterModel.Leagues = await GetLeagues(ConfigLogsFilterModel.SportId, ConfigLogsFilterModel.SegmentId);
            ConfigLogsFilterModel.MarketTemplateGroups = await GetMarketGroups(ConfigLogsFilterModel.FixtureTypeId, ConfigLogsFilterModel.SportId);
            ConfigLogsFilterModel.Markets = await GetMarkets(ConfigLogsFilterModel.FixtureTypeId, ConfigLogsFilterModel.SportId, ConfigLogsFilterModel.MarketTemplateIds);
            await ConfigLogsFilterModelChanged.InvokeAsync(ConfigLogsFilterModel);
            await GetContentCallback.InvokeAsync();
        }

        public async Task OnLeagueChangedEvent(ChangeEventArgs e)
        {
            ConfigLogsFilterModel.LeagueId = int.Parse(e.Value.ToString());
            ConfigLogsFilterModel.MarketTemplateGroups = await GetMarketGroups(ConfigLogsFilterModel.FixtureTypeId, ConfigLogsFilterModel.SportId);
            ConfigLogsFilterModel.Markets = await GetMarkets(ConfigLogsFilterModel.FixtureTypeId, ConfigLogsFilterModel.SportId, ConfigLogsFilterModel.MarketTemplateIds);
            await ConfigLogsFilterModelChanged.InvokeAsync(ConfigLogsFilterModel);
            await GetContentCallback.InvokeAsync();
        }

        public async Task OnMarketGroupChangedEvent(ChangeEventArgs e)
        {
            ConfigLogsFilterModel.MarketTemplateGroupId = int.Parse(e.Value.ToString());
            ConfigLogsFilterModel.MarketTemplateIds = await GetMarketTemplateIds(ConfigLogsFilterModel.FixtureTypeId, ConfigLogsFilterModel.SportId, ConfigLogsFilterModel.MarketTemplateGroupId);
            ConfigLogsFilterModel.Markets = await GetMarkets(ConfigLogsFilterModel.FixtureTypeId, ConfigLogsFilterModel.SportId, ConfigLogsFilterModel.MarketTemplateIds);
            await ConfigLogsFilterModelChanged.InvokeAsync(ConfigLogsFilterModel);
            await GetContentCallback.InvokeAsync();
        }

        public async Task OnMarketChangedEvent(ChangeEventArgs e)
        {
            ConfigLogsFilterModel.MarketId = e.Value.ToString();
            await ConfigLogsFilterModelChanged.InvokeAsync(ConfigLogsFilterModel);
            await GetContentCallback.InvokeAsync();
        }

        public async Task OnScopeChangedEvent(ChangeEventArgs e)
        {
            ConfigLogsFilterModel.Scope = e.Value.ToString();
            await ConfigLogsFilterModelChanged.InvokeAsync(ConfigLogsFilterModel);
            await GetContentCallback.InvokeAsync();
        }

        public async Task OnDateRangeChangedEvent(DateRange range)
        {
            ConfigLogsFilterModel.FromDate = DateTime.Parse(range.Start.ToString()).Date.Add(new TimeSpan(0, 0, 0));
            ConfigLogsFilterModel.ToDate = DateTime.Parse(range.End.ToString()).Date.Add(new TimeSpan(23, 59, 59));

            await ConfigLogsFilterModelChanged.InvokeAsync(ConfigLogsFilterModel);
            await GetContentCallback.InvokeAsync();
        }
        public void Dispose()
        {

        }
        #endregion

        #region Utilities
        private List<SelectListItemModel<int>> GetFixtureTypes()
        {
            return new List<SelectListItemModel<int>>()
            {
                new SelectListItemModel<int> { Value = 0, Name = "Match" },
                new SelectListItemModel<int> {  Value = 1, Name = "Outrights" }
            };
        }

        private async Task<List<SelectListItemModel<Guid>>> GetClients()
        {
            return (await TradingService.GetClientList()).Result.Where(x => UserClientCodes != null && UserClientCodes.Contains(x.ClientCode)).Select(c => new SelectListItemModel<Guid>()
            {
                Value = c.ClientId,
                Name = c.ClientName
            }).ToList();
        }

        private async Task<List<SelectListItemModel<int>>> GetSports()
        {
            return (await TradingService.GetSportLookup()).Result.SportLookup.Select(c => new SelectListItemModel<int>()
            {
                Value = c.SportId,
                Name = c.SportName
            }).ToList();
        }

        private async Task<List<SelectListItemModel<int>>> GetSegments(int sportId)
        {
            return (await TradingService.GetSegmentLookup(sportId)).Result.SegmentLookup.Select(c => new SelectListItemModel<int>()
            {
                Value = c.SegmentId,
                Name = c.SegmentName
            }).ToList();
        }

        private async Task<List<SelectListItemModel<int>>> GetLeagues(int sportId, int segmentId)
        {
            return (await TradingService.GetLeagueLookup(sportId, segmentId)).Result.LeagueLookup.Select(c => new SelectListItemModel<int>()
            {
                Value = c.LeagueId,
                Name = c.LeagueName
            }).ToList();
        }

        private async Task<List<SelectListItemModel<int>>> GetMarketGroups(int fixtureTypeId, int sportId)
        {
            var result = (await TradingService.GetMarketTemplateGroups(fixtureTypeId)).Result.Sports;

            var marketTemplateGroups = new List<MarketTemplateGroup>();
            if (sportId != 0)
            {
                var filtered = result?.FirstOrDefault(c => c.SportId == sportId);
                if (filtered != null)
                {
                    marketTemplateGroups.AddRange(filtered.Live);
                    marketTemplateGroups.AddRange(filtered.Prematch);
                }
            }

            return marketTemplateGroups.OrderBy(c => c.SortOrder).Select(c => new SelectListItemModel<int>()
            {
                Value = c.MarketTemplateGroupId,
                Name = c.MarketTemplateGroupName
            }).ToList();
        }

        private async Task<List<string>> GetMarketTemplateIds(int fixtureTypeId, int sportId, int marketGroupId)
        {
            var result = (await TradingService.GetMarketTemplateGroups(fixtureTypeId)).Result.Sports;

            var marketTemplateGroups = new List<MarketTemplateGroup>();
            var response = new List<string>();
            if (sportId != 0)
            {
                var filtered = result?.FirstOrDefault(c => c.SportId == sportId);
                if (filtered != null && marketGroupId != 0)
                {
                    marketTemplateGroups.AddRange(filtered.Live);
                    marketTemplateGroups.AddRange(filtered.Prematch);

                    marketTemplateGroups = marketTemplateGroups.Where(c => c.MarketTemplateGroupId == marketGroupId).ToList();

                    foreach (var item in marketTemplateGroups)
                    {
                        response.AddRange(item.MarketTemplateIds);
                    }
                }
            }

            return response;
        }

        private async Task<List<SelectListItemModel<string>>> GetMarkets(int fixtureTypeId, int sportId, List<string> marketTemplateIds)
        {
            var request = new MarketLookupRequest()
            {
                FixtureTypeId = fixtureTypeId,
                SportId = sportId
            };

            var result = (await TradingService.GetMarketLookup(request)).Result.MarketLookup;

            if (marketTemplateIds != null && marketTemplateIds.Any())
                result = result.Where(c => marketTemplateIds.Contains(c.MarketTemplateId)).ToList();

            return result.Select(c => new SelectListItemModel<string>()
            {
                Value = c.MarketTemplateId,
                Name = c.MarketTemplateName
            }).ToList();
        }

        private List<SelectListItemModel<string>> GetScopes()
        {
            return new List<SelectListItemModel<string>>()
            {
                new SelectListItemModel<string> { Value = "Prematch", Name = "Prematch" },
                new SelectListItemModel<string> {  Value = "Live", Name = "Live" }
            };
        }
        #endregion
    }
}
