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
        [Inject] IBackOfficeService BackOfficeService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] UserModel UserModel { get; set; }

        public FixtureType SelectedFixtureType { get; set; }
        public AddPriceBoostFilterModel Model { get; set; }
        public SportsTreeModel Sports { get; set; }        
        public MarketFixtureResponse MarketFixtureResponse { get; set; }

        public bool IsPreloader { get; set; }

        #endregion

        #region Fields

        public const string PreloaderInlineCss = "top:50%;transform:unset!important";

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            IsPreloader = true;
            SelectedFixtureType = FixtureType.Match;

            Model = new AddPriceBoostFilterModel();            

            Model.Sports = await GetSports(SelectedFixtureType);

            IsPreloader = false;
        }

        public async Task OnMatchTypeChanged(FixtureType fixtureType)
        {
            IsPreloader = true;
            SelectedFixtureType = fixtureType;

            Model = new AddPriceBoostFilterModel();
            Model.Sports = await GetSports(SelectedFixtureType);

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

            Model.PriceBoostConfigList = new List<PriceBoostConfigModel>();

            var clients = await GetClients();

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
                    FixtureTypeId = (int) SelectedFixtureType,
                    ProviderFixtureId = Model.ProviderFixtureId,
                    MarketTemplateId = Model.MarketTemplateId,
                    SelectionTemplateId = Model.SelectionTemplateId,
                    SelectionSpecifier = Model.SelectionSpecifier,
                    UserId = UserModel.Id,
                    UserName = UserModel.UserName,
                    TimeStamp = DateTime.Now,
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

            var options = new ModalOptions();

            options.HasHeaderBackButton = true;
            options.CallbackObject = Model;

            ModalService.Open<NextBoostModalBody>("Add Boosted Odds", parameters, options);
        }

        public async Task OnSportSelectChanged(ChangeEventArgs e)
        {
            IsPreloader = true;
            Model.SportId = int.Parse(e.Value.ToString());
            Model.SportName = Model.Sports.Where(s => s.Value == Model.SportId).FirstOrDefault().Name;

            Model.Segments = new List<SelectListItemModel<int>>();
            Model.Leagues = new List<SelectListItemModel<int>>();

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
                Model.MarketName = string.IsNullOrEmpty(Model.MarketTemplateId) ? "" : Model.Markets.Where(s => s.Value == $"{Model.MarketTemplateId}|{Model.MarketSpecifier}").FirstOrDefault().Name;
                Model.MarketName = GetMarketName(Model.MarketName, Model.MarketSpecifier);
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
            Model.SegmentId = int.Parse(e.Value.ToString());
            Model.SegmentName = Model.Segments.Where(s => s.Value == Model.SegmentId).FirstOrDefault().Name;

            Model.Leagues = new List<SelectListItemModel<int>>();

            Model.Leagues = await GetLeagues(Model.SportId, Model.SegmentId);
            Model.LeagueId = Model.Leagues.FirstOrDefault()?.Value ?? 0;
            Model.LeagueName = Model.Leagues.Where(s => s.Value == Model.LeagueId).FirstOrDefault().Name;

            Model.Fixtures = await GetFixtures(Model.SportId, Model.SegmentId, Model.LeagueId);
            Model.ProviderFixtureId = Model.Fixtures.FirstOrDefault()?.Value ?? string.Empty;
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

            IsPreloader = false;
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnLeagueSelectChanged(ChangeEventArgs e)
        {
            IsPreloader = true;
            Model.LeagueId = int.Parse(e.Value.ToString());
            Model.LeagueName = Model.Leagues.Where(s => s.Value == Model.LeagueId).FirstOrDefault().Name;

            Model.Fixtures = await GetFixtures(Model.SportId, Model.SegmentId, Model.LeagueId);
            Model.ProviderFixtureId = Model.Fixtures.FirstOrDefault()?.Value ?? string.Empty;
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

            IsPreloader = false;
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnFixtureSelectChanged(ChangeEventArgs e)
        {
            IsPreloader = true;
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

            IsPreloader = false;
            await InvokeAsync(StateHasChanged);
        }      

        public async Task OnMarketSelectChanged(ChangeEventArgs e)
        {
            IsPreloader = true;

            Model.MarketTemplateId = e.Value.ToString().Split('|')[0] ?? string.Empty;
            Model.MarketSpecifier = e.Value.ToString().Split('|')[1] ?? string.Empty;
            Model.MarketName = string.IsNullOrEmpty(Model.MarketTemplateId) ? "" : Model.Markets.Where(s => s.Value == $"{Model.MarketTemplateId}|{Model.MarketSpecifier}").FirstOrDefault().Name;
            Model.MarketName = GetMarketName(Model.MarketName, Model.MarketSpecifier);

            Model.Selections = await GetSelections(Model.ProviderFixtureId, Model.MarketTemplateId, Model.MarketSpecifier);
            Model.SelectionTemplateId = Model.Selections.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
            Model.SelectionSpecifier = Model.Selections.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
            Model.SelectionName = string.IsNullOrEmpty(Model.SelectionTemplateId) ? "" : Model.Selections.Where(s => s.Value == $"{Model.SelectionTemplateId}|{Model.SelectionSpecifier}").FirstOrDefault().Name;

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
        }

        public void Dispose()
        {
            
        }

        #endregion

        #region Utilities

        private async Task<List<SelectListItemModel<int>>> GetSports(FixtureType fixtureType)
        {
            Sports = new SportsTreeModel();

            Sports = (await TradingService.GetSports((int)fixtureType)).Result;

            if (Sports == null)
                return new List<SelectListItemModel<int>>();

            var sports = Sports.Sports?.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SportId,
                Name = s.SportName
            }).Distinct().ToList();

            return sports;
        }

        private async Task<List<SelectListItemModel<int>>> GetSegments(int sportId)
        {
            if (Sports == null)
                return new List<SelectListItemModel<int>>();

            var result = Sports.Sports.Where(s => s.SportId == sportId).FirstOrDefault();
            if (result == null)
                return new List<SelectListItemModel<int>>();

            var segments = result.Segments.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SegmentId,
                Name = s.SegmentName
            }).ToList();

            return segments;
        }

        private async Task<List<SelectListItemModel<int>>> GetLeagues(int sportId, int segmentId)
        {
            if (Sports == null)
                return new List<SelectListItemModel<int>>();

            var sportResult = Sports.Sports.Where(s => s.SportId == sportId).FirstOrDefault();
            if (sportResult == null)
                return new List<SelectListItemModel<int>>();

            var segmentResult = sportResult.Segments.Where(s => s.SegmentId == segmentId).FirstOrDefault();
            if (segmentResult == null)
                return new List<SelectListItemModel<int>>();

            var leagues = segmentResult.Leagues.Select(s => new SelectListItemModel<int>()
            {
                Value = s.LeagueId,
                Name = s.LeagueName
            }).ToList();

            return leagues;
        }

        private async Task<List<SelectListItemModel<string>>> GetFixtures(int sportId, int segmentId, int leagueId)
        {            
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

            if (MarketFixtureResponse == null)
                return new List<SelectListItemModel<string>>();

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
            }).Distinct().ToList();

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

        private async Task<List<ClientInformationModel>> GetClients()
        {
            var clients = (await TradingService.GetClientList()).Result;

            clients = clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode)).ToList();

            if (!string.IsNullOrEmpty(Model.ClientCode))
                clients = clients.Where(c => c.ClientCode == Model.ClientCode).ToList();

            return clients;
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
