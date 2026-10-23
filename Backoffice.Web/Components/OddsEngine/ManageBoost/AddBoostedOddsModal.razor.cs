using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.ManageBoost
{
    public partial class AddBoostedOddsModal : IDisposable
    {
        #region Properties

        [Inject] IModalService ModalService { get; set; }
        [Inject] ITradingService TradingService { get; set; }
        [Inject] IBackofficeService BackofficeService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        public FixtureType SelectedFixtureType { get; set; }
        public AddPriceBoostFilterModel Model { get; set; }
        public MarketFixtureResponse MarketFixtureResponse { get; set; }
        private string Region { get; set; }

        public bool IsPreloader { get; set; }
        #endregion

        #region Fields

        public const string PreloaderInlineCss = "transform: unset !important;position: absolute;display: flex;justify-content: center;align-items: center;z-index: 1000;top: 0;left: 0;right: 0;bottom: 0;background-color: rgba(0, 0, 0, 0.3);";

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            IsPreloader = true;
            SelectedFixtureType = FixtureType.Match;

            Model = new AddPriceBoostFilterModel();


            TradingService.Region = Region = ModalParameters.TryGet<string>("Region").ToUpper();

            // Back from the next dialog hands the earlier choices back
            var previous = ModalParameters.TryGet<AddPriceBoostFilterModel>("AddPriceBoostFilterModel");
            if (previous != null)
                await RestoreChoices(previous);

            IsPreloader = false;
        }

        protected override async Task OnParametersSetAsync()
        {
            Model.Sports = await GetSports();
        }

        public async Task OnMatchTypeChanged(FixtureType fixtureType)
        {
            IsPreloader = true;
            SelectedFixtureType = fixtureType;

            Model = new AddPriceBoostFilterModel();
            Model.Sports = await GetSports();

            IsPreloader = false;
        }

        public async Task OnCancelModalClick()
        {
            ModalService.Cancel();
        }

        public async Task OnNextModalClick()
        {
            if (Model.SportId == 0 ||
                Model.SegmentId == 0 ||
                Model.LeagueId == 0 ||
                string.IsNullOrEmpty(Model.ProviderFixtureId) ||
                string.IsNullOrEmpty(Model.MarketTemplateId) ||
                string.IsNullOrEmpty($"{Model.SelectionTemplateId}{Model.SelectionSpecifier}"))
            {
                return;
            }

            Model.FixtureTypeId = (int)SelectedFixtureType;
            Model.PriceBoostConfigList = new List<PriceBoostConfigModel>();

            var clients = _userState.Model.TradingClients;

            var priceBoosts = (await TradingService.GetPriceBoostConfig(new PriceBoostConfigRequest
            {
                ClientCode = Model.ClientCode,
                SportId = Model.SportId.ToString(),
                SegmentId = Model.SegmentId.ToString(),
                LeagueId = Model.LeagueId.ToString(),
                ProviderFixtureId = Model.ProviderFixtureId.ToString(),
                MarketTemplateId = Model.MarketTemplateId.ToString(),
                SelectionTemplateId = Model.SelectionTemplateId.ToString(),
                SelectionSpecifier = Model.SelectionSpecifier,
                FixtureTypeId = (int)SelectedFixtureType,
                MarketSpecifier = Model.MarketSpecifier
            })).Result;

            foreach (var client in clients)
            {
                var config = new PriceBoostConfigModel
                {
                    ClientCode = client.ClientCode,
                    ClientId = client.ClientId.ToString(),
                    ClientName = client.ClientName,
                    SportId = Model.SportId,
                    LeagueId = Model.LeagueId,
                    SegmentId = Model.SegmentId,
                    FixtureTypeId = (int)SelectedFixtureType,
                    ProviderFixtureId = Model.ProviderFixtureId,
                    MarketTemplateId = Model.MarketTemplateId,
                    SelectionTemplateId = Model.SelectionTemplateId,
                    SelectionSpecifier = Model.SelectionSpecifier,
                    UserId = _userState.Model.Id,
                    UserName = _userState.Model.Name,
                    TimeStamp = DateTime.UtcNow,
                    MarketSpecifier = Model.MarketSpecifier
                };

                if (priceBoosts != null)
                {
                    var priceBoost = priceBoosts.FirstOrDefault(c => c.ClientCode == client.ClientCode);

                    if (priceBoost != null)
                    {
                        config.FixtureName = priceBoost.FixtureName;
                        config.SportId = priceBoost.SportId;
                        config.SportName = priceBoost.SportName;
                        config.SegmentId = priceBoost.SegmentId;
                        config.SegmentName = priceBoost.SegmentName;
                        config.LeagueId = priceBoost.LeagueId;
                        config.LeagueName = priceBoost.LeagueName;
                        config.SelectionTemplateId = priceBoost.SelectionTemplateId;
                        config.SelectionSpecifier = priceBoost.SelectionSpecifier;
                        config.SelectionName = priceBoost.SelectionName;
                        config.MarketName = priceBoost.MarketName;
                        config.MarketTemplateId = priceBoost.MarketTemplateId;
                        config.CurrentPeriod = priceBoost.CurrentPeriod;
                        config.CurrentMinute = priceBoost.CurrentMinute;
                        config.FixtureStartDate = priceBoost.FixtureStartDate;
                        config.FixtureStatusId = priceBoost.FixtureStatusId;
                        config.Score = priceBoost.Score;

                        config.BROdds = priceBoost.BROdds;
                        config.ClientOdds = priceBoost.ClientOdds;
                        config.BoostedOdds = priceBoost.BoostedOdds;

                        if (priceBoost.BoostedOdds != 0)
                            config.BoostedOddsStr = priceBoost.BoostedOdds.ToString("0.00");

                        config.BoostedSelectionName = priceBoost.BoostedSelectionName;
                        config.BoostedSelectionTranslations = priceBoost.BoostedSelectionTranslations;
                    }
                }

                Model.PriceBoostConfigList.Add(config);
            }

            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("AddPriceBoostFilterModel", Model);
            parameters.Add("Region", Region);

            var options = new ModalOptions();

            options.HasHeaderBackButton = true;
            options.CallbackObject = Model;

            ModalService.Open<NextBoostModalBody>("Add Boosted Odds", parameters, options);
        }

        public async Task OnSportSelectChanged(ChangeEventArgs e)
        {
            IsPreloader = true;
            Model.SportId = int.Parse(e.Value.ToString());
            ClearBelow(Level.Sport);

            if (Model.SportId == 0)
            {
                Model.SportName = string.Empty;

                IsPreloader = false;
                await InvokeAsync(StateHasChanged);
                return;
            }

            Model.SportName = Model.Sports.Where(s => s.Value == Model.SportId).FirstOrDefault().Name;

            Model.Segments = await GetSegments(Model.SportId);
            if (Model.Segments.Count > 0)
            {
                Model.SegmentId = Model.Segments.FirstOrDefault()?.Value ?? 0;
                Model.SegmentName = Model.Segments.Where(s => s.Value == Model.SegmentId).FirstOrDefault().Name;
            }

            Model.Leagues = await GetLeagues(Model.SportId, Model.SegmentId);
            if (Model.Leagues.Count > 0)
            {
                Model.LeagueId = Model.Leagues.FirstOrDefault()?.Value ?? 0;
                Model.LeagueName = Model.Leagues.Where(s => s.Value == Model.LeagueId).FirstOrDefault().Name;
            }

            Model.Fixtures = await GetFixtures(Model.SportId, Model.SegmentId, Model.LeagueId);
            if (Model.Fixtures.Count > 0)
            {
                Model.ProviderFixtureId = Model.Fixtures.FirstOrDefault()?.Value ?? string.Empty;
                Model.FixtureName = string.IsNullOrEmpty(Model.ProviderFixtureId) ? "" : Model.Fixtures.Where(s => s.Value == Model.ProviderFixtureId).FirstOrDefault().Name;
            }

            Model.Markets = await GetMarkets(Model.ProviderFixtureId);
            if (Model.Markets.Count > 0)
            {
                Model.MarketTemplateId = Model.Markets.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
                Model.MarketSpecifier = Model.Markets.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
                var marketName = string.IsNullOrEmpty(Model.MarketTemplateId) ? "" : Model.Markets.Where(s => s.Value == $"{Model.MarketTemplateId}|{Model.MarketSpecifier}").FirstOrDefault().Name;
                Model.MarketName = GetMarketName(marketName, Model.MarketSpecifier);
            }

            Model.Selections = await GetSelections(Model.ProviderFixtureId, Model.MarketTemplateId, Model.MarketSpecifier);
            if (Model.Selections.Count > 0)
            {
                Model.SelectionTemplateId = Model.Selections.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
                Model.SelectionSpecifier = Model.Selections.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
                Model.SelectionName = string.IsNullOrEmpty(Model.SelectionTemplateId) ? "" : Model.Selections.Where(s => s.Value == $"{Model.SelectionTemplateId}|{Model.SelectionSpecifier}").FirstOrDefault().Name;
            }

            IsPreloader = false;
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnSegmentSelectChanged(ChangeEventArgs e)
        {
            IsPreloader = true;

            if (!string.IsNullOrEmpty(e.Value.ToString()) && e.Value.ToString() != "0")
            {
                Model.SegmentId = int.Parse(e.Value.ToString());
                Model.SegmentName = Model.Segments.Where(s => s.Value == Model.SegmentId).FirstOrDefault().Name;
                ClearBelow(Level.Segment);

                Model.Leagues = await GetLeagues(Model.SportId, Model.SegmentId) ?? new List<SelectListItemModel<int>>();
                if (Model.Leagues.Any())
                {
                    Model.LeagueId = Model.Leagues.FirstOrDefault()?.Value ?? 0;
                    Model.LeagueName = Model.Leagues.Where(s => s.Value == Model.LeagueId).FirstOrDefault()?.Name;
                }

                Model.Fixtures = await GetFixtures(Model.SportId, Model.SegmentId, Model.LeagueId) ?? new List<SelectListItemModel<string>>();
                if (Model.Fixtures.Any())
                {
                    Model.ProviderFixtureId = Model.Fixtures.FirstOrDefault()?.Value ?? string.Empty;
                    Model.FixtureName = string.IsNullOrEmpty(Model.ProviderFixtureId) ? "" : Model.Fixtures.Where(s => s.Value == Model.ProviderFixtureId).FirstOrDefault().Name;
                }

                Model.Markets = await GetMarkets(Model.ProviderFixtureId) ?? new List<SelectListItemModel<string>>();
                if (Model.Markets.Any())
                {
                    Model.MarketTemplateId = Model.Markets.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
                    Model.MarketSpecifier = Model.Markets.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
                    var marketName = string.IsNullOrEmpty(Model.MarketTemplateId) ? "" : Model.Markets.Where(s => s.Value == $"{Model.MarketTemplateId}|{Model.MarketSpecifier}").FirstOrDefault().Name;
                    Model.MarketName = GetMarketName(marketName, Model.MarketSpecifier);
                }

                Model.Selections = await GetSelections(Model.ProviderFixtureId, Model.MarketTemplateId, Model.MarketSpecifier) ?? new List<SelectListItemModel<string>>();
                if (Model.Selections.Any())
                {
                    Model.SelectionTemplateId = Model.Selections.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
                    Model.SelectionSpecifier = Model.Selections.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
                    Model.SelectionName = string.IsNullOrEmpty(Model.SelectionTemplateId) ? "" : Model.Selections.Where(s => s.Value == $"{Model.SelectionTemplateId}|{Model.SelectionSpecifier}").FirstOrDefault().Name;
                }
            }
            else
            {
                Model.SegmentId = 0;
                Model.SegmentName = string.Empty;
                ClearBelow(Level.Segment);
            }

            IsPreloader = false;
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnLeagueSelectChanged(ChangeEventArgs e)
        {
            IsPreloader = true;

            if (!string.IsNullOrEmpty(e.Value.ToString()) && e.Value.ToString() != "0")
            {
                Model.LeagueId = int.Parse(e.Value.ToString());
                Model.LeagueName = Model.Leagues.Where(s => s.Value == Model.LeagueId).FirstOrDefault().Name;

                Model.Fixtures = await GetFixtures(Model.SportId, Model.SegmentId, Model.LeagueId);
                Model.ProviderFixtureId = Model.Fixtures.FirstOrDefault()?.Value ?? string.Empty;
                Model.FixtureName = string.IsNullOrEmpty(Model.ProviderFixtureId) ? "" : Model.Fixtures.Where(s => s.Value == Model.ProviderFixtureId).FirstOrDefault().Name;

                Model.Markets = await GetMarkets(Model.ProviderFixtureId);
                Model.MarketTemplateId = Model.Markets.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
                Model.MarketSpecifier = Model.Markets.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
                var marketName = string.IsNullOrEmpty(Model.MarketTemplateId) ? "" : Model.Markets.Where(s => s.Value == $"{Model.MarketTemplateId}|{Model.MarketSpecifier}").FirstOrDefault().Name;
                Model.MarketName = GetMarketName(marketName, Model.MarketSpecifier);

                Model.Selections = await GetSelections(Model.ProviderFixtureId, Model.MarketTemplateId, Model.MarketSpecifier);
                Model.SelectionTemplateId = Model.Selections.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
                Model.SelectionSpecifier = Model.Selections.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
                Model.SelectionName = string.IsNullOrEmpty(Model.SelectionTemplateId) ? "" : Model.Selections.Where(s => s.Value == $"{Model.SelectionTemplateId}|{Model.SelectionSpecifier}").FirstOrDefault().Name;
            }
            else
            {
                Model.LeagueId = 0;
                Model.LeagueName = string.Empty;
                ClearBelow(Level.League);
            }

            IsPreloader = false;
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnFixtureSelectChanged(ChangeEventArgs e)
        {
            IsPreloader = true;

            if (!string.IsNullOrEmpty(e.Value.ToString()) && e.Value.ToString() != "0")
            {
                Model.ProviderFixtureId = e.Value.ToString();
                Model.FixtureName = string.IsNullOrEmpty(Model.ProviderFixtureId) ? "" : Model.Fixtures.Where(s => s.Value == Model.ProviderFixtureId).FirstOrDefault().Name;

                Model.Markets = await GetMarkets(Model.ProviderFixtureId);
                Model.MarketTemplateId = Model.Markets.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
                Model.MarketSpecifier = Model.Markets.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
                Model.MarketName = string.IsNullOrEmpty(Model.MarketTemplateId) ? "" : Model.Markets.Where(s => s.Value == $"{Model.MarketTemplateId}|{Model.MarketSpecifier}").FirstOrDefault().Name;
                Model.MarketName = GetMarketName(Model.MarketName, Model.MarketSpecifier);

                Model.Selections = await GetSelections(Model.ProviderFixtureId, Model.MarketTemplateId, Model.MarketSpecifier);
                Model.SelectionTemplateId = Model.Selections.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
                Model.SelectionSpecifier = Model.Selections.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
                Model.SelectionName = string.IsNullOrEmpty(Model.SelectionTemplateId) ? "" : Model.Selections.Where(s => s.Value == $"{Model.SelectionTemplateId}|{Model.SelectionSpecifier}").FirstOrDefault().Name;
            }
            else
            {
                Model.ProviderFixtureId = string.Empty;
                Model.FixtureName = string.Empty;
                ClearBelow(Level.Fixture);
            }

            IsPreloader = false;
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnMarketSelectChanged(ChangeEventArgs e)
        {
            IsPreloader = true;

            if (!string.IsNullOrEmpty(e.Value.ToString()) && e.Value.ToString() != "0")
            {
                Model.MarketTemplateId = e.Value.ToString().Split('|')[0] ?? string.Empty;
                Model.MarketSpecifier = e.Value.ToString().Split('|')[1] ?? string.Empty;
                Model.MarketName = string.IsNullOrEmpty(Model.MarketTemplateId) ? "" : Model.Markets.Where(s => s.Value == $"{Model.MarketTemplateId}|{Model.MarketSpecifier}").FirstOrDefault().Name;
                Model.MarketName = GetMarketName(Model.MarketName, Model.MarketSpecifier);

                Model.Selections = await GetSelections(Model.ProviderFixtureId, Model.MarketTemplateId, Model.MarketSpecifier);
                Model.SelectionTemplateId = Model.Selections.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
                Model.SelectionSpecifier = Model.Selections.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
                Model.SelectionName = string.IsNullOrEmpty(Model.SelectionTemplateId) ? "" : Model.Selections.Where(s => s.Value == $"{Model.SelectionTemplateId}|{Model.SelectionSpecifier}").FirstOrDefault().Name;
            }
            else
            {
                Model.MarketTemplateId = string.Empty;
                Model.MarketSpecifier = string.Empty;
                Model.MarketName = string.Empty;
                ClearBelow(Level.Market);
            }

            IsPreloader = false;
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnSelectionSelectChanged(ChangeEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Value.ToString()))
            {
                Model.SelectionTemplateId = e.Value.ToString();
                Model.SelectionTemplateId = e.Value.ToString().Split('|')[0] ?? string.Empty;
                Model.SelectionSpecifier = e.Value.ToString().Split('|')[1] ?? string.Empty;
                Model.SelectionName = string.IsNullOrEmpty(Model.SelectionTemplateId) ? "" : Model.Selections.Where(c => c.Value == $"{Model.SelectionTemplateId}|{Model.SelectionSpecifier}").FirstOrDefault().Name;
            }
            else
            {
                Model.SelectionTemplateId = string.Empty;
                Model.SelectionSpecifier = string.Empty;
                Model.SelectionName = string.Empty;
            }
        }

        public void Dispose()
        {

        }

        #endregion

        #region Utilities

        private enum Level
        {
            Sport,
            Segment,
            League,
            Fixture,
            Market
        }

        // Puts back the choices made before Next, on the tab they were made on. The market and selection lists are read
        // from the last fixture response, which this new dialog does not have, so the fixtures are loaded again. A
        // fixture that is no longer listed (it started in the meantime) takes the dropdowns below it with it.
        private async Task RestoreChoices(AddPriceBoostFilterModel previous)
        {
            Model = previous;
            SelectedFixtureType = (FixtureType)previous.FixtureTypeId;

            if (Model.LeagueId == 0)
                return;

            var fixtures = await GetFixtures(Model.SportId, Model.SegmentId, Model.LeagueId);

            if (!fixtures.Any(f => f.Value == Model.ProviderFixtureId))
                ClearBelow(Level.League);

            Model.Fixtures = fixtures;
        }

        // Empties every dropdown below the one that changed, with its chosen value, so a choice that is no longer
        // visible cannot still be submitted with Next.
        private void ClearBelow(Level changed)
        {
            if (changed < Level.Segment)
            {
                Model.Segments = new List<SelectListItemModel<int>>();
                Model.SegmentId = 0;
                Model.SegmentName = string.Empty;
            }

            if (changed < Level.League)
            {
                Model.Leagues = new List<SelectListItemModel<int>>();
                Model.LeagueId = 0;
                Model.LeagueName = string.Empty;
            }

            if (changed < Level.Fixture)
            {
                Model.Fixtures = new List<SelectListItemModel<string>>();
                Model.ProviderFixtureId = string.Empty;
                Model.FixtureName = string.Empty;
            }

            if (changed < Level.Market)
            {
                Model.Markets = new List<SelectListItemModel<string>>();
                Model.MarketTemplateId = string.Empty;
                Model.MarketSpecifier = string.Empty;
                Model.MarketName = string.Empty;
            }

            Model.Selections = new List<SelectListItemModel<string>>();
            Model.SelectionTemplateId = string.Empty;
            Model.SelectionSpecifier = string.Empty;
            Model.SelectionName = string.Empty;
        }

        private async Task<List<SelectListItemModel<int>>> GetSports()
        {
            var response = await BackofficeService.GetSportLookup(_translationState.ActiveLanguageCode.ToString());

            var sportsLookup = response.Result;
            if (sportsLookup == null)
                return new List<SelectListItemModel<int>>();

            return sportsLookup.SportLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SportId,
                Name = s.SportName
            }).ToList();
        }

        private async Task<List<SelectListItemModel<int>>> GetSegments(int sportId)
        {
            var response = await BackofficeService.GetSegmentLookup(sportId, _translationState.ActiveLanguageCode);

            var segmentsLookup = response.Result;
            if (segmentsLookup == null)
                return new List<SelectListItemModel<int>>();

            return segmentsLookup.SegmentLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SegmentId,
                Name = s.SegmentName
            }).ToList();
        }

        private async Task<List<SelectListItemModel<int>>> GetLeagues(int sportId, int segmentId)
        {
            var response = await BackofficeService.GetLeagueLookup(sportId, segmentId, _translationState.ActiveLanguageCode);

            var leaguesLookup = response.Result;
            if (leaguesLookup == null)
                return new List<SelectListItemModel<int>>();

            return leaguesLookup.LeagueLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.LeagueId,
                Name = s.LeagueName
            }).ToList();
        }

        private async Task<List<SelectListItemModel<string>>> GetFixtures(int sportId, int segmentId, int leagueId)
        {
            MarketFixtureResponse = null;

            if (leagueId == 0)
                return new List<SelectListItemModel<string>>();

            if (SelectedFixtureType == FixtureType.Match)
            {
                MarketFixtureResponse = (await TradingService.GetSportsBookFixtures(new MarketFixtureRequest
                {
                    FixtureTypeId = (int)SelectedFixtureType,
                    LeagueId = leagueId,
                    SegmentId = segmentId,
                    SportId = sportId,
                })).Result;

            }
            else
            {
                MarketFixtureResponse = (await TradingService.GetSportsBookOutrightFixtures(new MarketFixtureRequest
                {
                    FixtureTypeId = (int)SelectedFixtureType,
                    LeagueId = leagueId,
                    SegmentId = segmentId,
                    SportId = sportId,
                })).Result;

            }

            if (MarketFixtureResponse?.Fixtures == null)
            {
                MarketFixtureResponse = null;
                return new List<SelectListItemModel<string>>();
            }

            if (MarketFixtureResponse.Fixtures.Count > 0)
                MarketFixtureResponse.Fixtures = MarketFixtureResponse.Fixtures.Where(f => f.FixtureStatusId < 1).ToList();

            if (MarketFixtureResponse.Fixtures.Count == 0)
                return new List<SelectListItemModel<string>>();

            var fixtures = MarketFixtureResponse.Fixtures.Select(s => new SelectListItemModel<string>
            {
                Value = s.ProviderFixtureId,
                Name = s.FixtureName
            }).Distinct()
            .ToList();

            return fixtures.OrderBy(o => o.Name).ToList();
        }

        private async Task<List<SelectListItemModel<string>>> GetMarkets(string providerFixtureId)
        {
            if (MarketFixtureResponse == null)
                return new List<SelectListItemModel<string>>();

            var fixture = MarketFixtureResponse.Fixtures.Where(f => f.ProviderFixtureId == providerFixtureId).FirstOrDefault();
            if (fixture == null)
                return new List<SelectListItemModel<string>>();

            var markets = fixture.Markets?.GroupBy(g => $"{g.MarketTemplateId}|{g.MarketSpecifier}").Select(s => new SelectListItemModel<string>()
            {
                Value = s.Key,
                Name = GetMarketName(s.FirstOrDefault().MarketName, s.FirstOrDefault().MarketSpecifier)
            }).Distinct().ToList() ?? new List<SelectListItemModel<string>>();

            return markets.OrderBy(m => m.Name).ToList();
        }

        private async Task<List<SelectListItemModel<string>>> GetSelections(string providerFixtureId, string marketTemplateId, string marketSpecifier)
        {
            if (MarketFixtureResponse == null)
                return new List<SelectListItemModel<string>>();

            var fixture = MarketFixtureResponse.Fixtures.Where(f => f.ProviderFixtureId == providerFixtureId).FirstOrDefault();
            if (fixture == null)
                return new List<SelectListItemModel<string>>();

            var markets = fixture.Markets.Where(m => m.MarketTemplateId == marketTemplateId && m.Selections.Any(s => string.IsNullOrEmpty(marketSpecifier) || s.SelectionSpecifier.Contains(marketSpecifier)));
            if (markets == null)
                return new List<SelectListItemModel<string>>();

            var selections = new List<SelectListItemModel<string>>();

            foreach (var market in markets)
            {
                var tempSelections = market.Selections.Select(s => new SelectListItemModel<string>()
                {
                    Value = $"{s.SelectionTemplateId}|{s.SelectionSpecifier}",
                    Name = s.SelectionName
                }).Distinct().ToList();

                selections.AddRange(tempSelections);
            }

            return selections.OrderBy(m => m.Name).ToList();
        }
        private string GetMarketName(string marketName, string marketSpecifier)
        {
            if (string.IsNullOrEmpty(marketName))
                return string.Empty;

            if (string.IsNullOrEmpty(marketSpecifier))
                return marketName;

            if (!marketSpecifier.Contains("="))
                return marketName;

            var marketLine = marketSpecifier.Split("=")[1];

            if (!marketName.Contains(marketLine) && marketLine != "0")
                return $"{marketName} {marketLine}";
            else
                return marketName;
        }

        #endregion
    }
}
