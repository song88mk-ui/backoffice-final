using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
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
        public AddPriceBoostFilterModel AddPriceBoostFilterModel { get; set; }
        public SportsTreeModel Sports { get; set; }        
        public MarketFixtureResponse marketFixtureresponse { get; set; }
        public bool IsPreloader { get; set; }
        public const string PreloaderInlineCss = "top:50%;transform:unset!important";        
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            IsPreloader = true;
            SelectedFixtureType = FixtureType.Match;

            AddPriceBoostFilterModel = ModalParameters.TryGet<AddPriceBoostFilterModel>("AddPriceBoostFilterModel");            

            if (AddPriceBoostFilterModel == null)
            {
                AddPriceBoostFilterModel = new AddPriceBoostFilterModel();
                AddPriceBoostFilterModel.Sports = await GetSports(SelectedFixtureType);
            }

            IsPreloader = false;
        }
        public async Task OnMatchTypeChanged(FixtureType fixtureType)
        {
            IsPreloader = true;
            SelectedFixtureType = fixtureType;

            AddPriceBoostFilterModel = new AddPriceBoostFilterModel();
            AddPriceBoostFilterModel.Sports = await GetSports(SelectedFixtureType);

            IsPreloader = false;
            await InvokeAsync(StateHasChanged);
        }
        public async Task OnCancelModalClick()
        {
            ModalService.Cancel();
        }

        public async Task OnNextModalClick()
        {
            if (AddPriceBoostFilterModel.SportId == 0 ||
                AddPriceBoostFilterModel.SegmentId == 0 ||
                AddPriceBoostFilterModel.LeagueId == 0 ||
                string.IsNullOrEmpty(AddPriceBoostFilterModel.ProviderFixtureId) ||
                string.IsNullOrEmpty(AddPriceBoostFilterModel.MarketTemplateId) ||
                string.IsNullOrEmpty($"{AddPriceBoostFilterModel.SelectionTemplateId}{AddPriceBoostFilterModel.SelectionSpecifier}"))
            {
                //ShowErrorMessage = true;
                //AlertMessage = "Please select all options.";
                //AlertStatus = "Error!";
                //Opacity = 1.0m;
                return;
            }

            AddPriceBoostFilterModel.PriceBoostConfigList = new List<Backoffice.Shared.Models.PriceBoostConfigModel>();
            var clientModel = await GetClients();
            clientModel.Clients = clientModel.Clients.Where(x => UserModel.ClientCodes.Contains(x.ClientCode)).ToList();

            if (!string.IsNullOrEmpty(AddPriceBoostFilterModel.ClientCode))
                clientModel.Clients = clientModel.Clients.Where(c => c.ClientCode == AddPriceBoostFilterModel.ClientCode).ToList();

            var priceBoostList = (await TradingService.GetPriceBoostConfig(new PriceBoostConfigRequest()
            {
                ClientCode = AddPriceBoostFilterModel.ClientCode,
                SportId = AddPriceBoostFilterModel.SportId.ToString(),
                SegmentId = AddPriceBoostFilterModel.SegmentId.ToString(),
                LeagueId = AddPriceBoostFilterModel.LeagueId.ToString(),
                ProviderFixtureId = AddPriceBoostFilterModel.ProviderFixtureId.ToString(),
                MarketTemplateId = AddPriceBoostFilterModel.MarketTemplateId.ToString(),
                SelectionTemplateId = AddPriceBoostFilterModel.SelectionTemplateId.ToString(),
                SelectionSpecifier = AddPriceBoostFilterModel.SelectionSpecifier,
                FixtureTypeId = (int)SelectedFixtureType,
                MarketSpecifier = AddPriceBoostFilterModel.MarketSpecifier
            })).Result;

            foreach (var client in clientModel.Clients)
            {
                var priceboostConfig = new Backoffice.Shared.Models.PriceBoostConfigModel()
                {
                    ClientCode = client.ClientCode,
                    ClientId = client.ClientId.ToString(),
                    ClientName = client.ClientName,
                    SportId = AddPriceBoostFilterModel.SportId,
                    LeagueId = AddPriceBoostFilterModel.LeagueId,
                    SegmentId = AddPriceBoostFilterModel.SegmentId,
                    FixtureTypeId = (int) SelectedFixtureType,
                    ProviderFixtureId = AddPriceBoostFilterModel.ProviderFixtureId,
                    MarketTemplateId = AddPriceBoostFilterModel.MarketTemplateId,
                    SelectionTemplateId = AddPriceBoostFilterModel.SelectionTemplateId,
                    SelectionSpecifier = AddPriceBoostFilterModel.SelectionSpecifier,
                    UserId = UserModel.Id,
                    UserName = UserModel.UserName,
                    TimeStamp = DateTime.Now,
                    MarketSpecifier = AddPriceBoostFilterModel.MarketSpecifier
                };

                if (priceBoostList != null)
                {
                    var priceBoost = priceBoostList.Where(p => p.ClientCode == client.ClientCode).FirstOrDefault();
                    if (priceBoost != null)
                    {
                        priceboostConfig.FixtureName = priceBoost.FixtureName;
                        priceboostConfig.SportId = priceBoost.SportId;
                        priceboostConfig.SportName = priceBoost.SportName;
                        priceboostConfig.SegmentId = priceBoost.SegmentId;
                        priceboostConfig.SegmentName = priceBoost.SegmentName;
                        priceboostConfig.LeagueId = priceBoost.LeagueId;
                        priceboostConfig.LeagueName = priceBoost.LeagueName;
                        priceboostConfig.SelectionTemplateId = priceBoost.SelectionTemplateId;
                        priceboostConfig.SelectionSpecifier = priceBoost.SelectionSpecifier;
                        priceboostConfig.SelectionName = priceBoost.SelectionName;
                        priceboostConfig.MarketName = priceBoost.MarketName;
                        priceboostConfig.MarketTemplateId = priceBoost.MarketTemplateId;
                        priceboostConfig.CurrentPeriod = priceBoost.CurrentPeriod;
                        priceboostConfig.CurrentMinute = priceBoost.CurrentMinute;
                        priceboostConfig.FixtureStartDate = priceBoost.FixtureStartDate;
                        priceboostConfig.FixtureStatusId = priceBoost.FixtureStatusId;
                        priceboostConfig.Score = priceBoost.Score;

                        priceboostConfig.BROdds = priceBoost.BROdds;
                        priceboostConfig.ClientOdds = priceBoost.ClientOdds;
                        priceboostConfig.BoostedOdds = priceBoost.BoostedOdds;
                        if (priceBoost.BoostedOdds != 0)
                            priceboostConfig.BoostedOddsStr = priceBoost.BoostedOdds.ToString("0.00");

                        priceboostConfig.BoostedSelectionName = priceBoost.BoostedSelectionName;
                        priceboostConfig.BoostedSelectionTranslations = priceBoost.BoostedSelectionTranslations;
                    }
                }

                AddPriceBoostFilterModel.PriceBoostConfigList.Add(priceboostConfig);
            }

            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("AddPriceBoostFilterModel", AddPriceBoostFilterModel);

            var options = new ModalOptions();
            options.HasHeaderBackButton = true;
            options.CallbackObject = AddPriceBoostFilterModel;
            //ModalService.Open<NextBoostModalBody>($"Add Boosted Odds - {(AddPriceBoostFilterModel.ShowMatchTypeMenu ? "Match Type" : "Outright")}", parameters, options);
            ModalService.Open<NextBoostModalBody>("Add Boosted Odds", parameters, options);
        }

        public async Task OnSportSelectChanged(ChangeEventArgs e)        
        {
            IsPreloader = true;
            AddPriceBoostFilterModel.SportId = int.Parse(e.Value.ToString());
            AddPriceBoostFilterModel.SportName = AddPriceBoostFilterModel.Sports.Where(s => s.Value == AddPriceBoostFilterModel.SportId).FirstOrDefault().Name;

            AddPriceBoostFilterModel.Segments = new List<SelectListItemModel<int>>();
            AddPriceBoostFilterModel.Leagues = new List<SelectListItemModel<int>>();

            AddPriceBoostFilterModel.Segments = await GetSegments(AddPriceBoostFilterModel.SportId);
            if (AddPriceBoostFilterModel.Segments.Count > 0)
            {
                AddPriceBoostFilterModel.SegmentId = AddPriceBoostFilterModel.Segments.FirstOrDefault()?.Value ?? 0;
                AddPriceBoostFilterModel.SegmentName = AddPriceBoostFilterModel.Segments.Where(s => s.Value == AddPriceBoostFilterModel.SegmentId).FirstOrDefault().Name;
            }

            AddPriceBoostFilterModel.Leagues = await GetLeagues(AddPriceBoostFilterModel.SportId, AddPriceBoostFilterModel.SegmentId);
            if (AddPriceBoostFilterModel.Leagues.Count > 0)
            {
                AddPriceBoostFilterModel.LeagueId = AddPriceBoostFilterModel.Leagues.FirstOrDefault()?.Value ?? 0;
                AddPriceBoostFilterModel.LeagueName = AddPriceBoostFilterModel.Leagues.Where(s => s.Value == AddPriceBoostFilterModel.LeagueId).FirstOrDefault().Name;
            }

            AddPriceBoostFilterModel.Fixtures = await GetFixtures(AddPriceBoostFilterModel.SportId, AddPriceBoostFilterModel.SegmentId, AddPriceBoostFilterModel.LeagueId);
            if (AddPriceBoostFilterModel.Fixtures.Count > 0)
            {
                AddPriceBoostFilterModel.ProviderFixtureId = AddPriceBoostFilterModel.Fixtures.FirstOrDefault()?.Value ?? string.Empty;
                AddPriceBoostFilterModel.FixtureName = string.IsNullOrEmpty(AddPriceBoostFilterModel.ProviderFixtureId) ? "" : AddPriceBoostFilterModel.Fixtures.Where(s => s.Value == AddPriceBoostFilterModel.ProviderFixtureId).FirstOrDefault().Name;
            }

            AddPriceBoostFilterModel.Markets = await GetMarkets(AddPriceBoostFilterModel.ProviderFixtureId);
            if (AddPriceBoostFilterModel.Markets.Count > 0)
            {
                AddPriceBoostFilterModel.MarketTemplateId = AddPriceBoostFilterModel.Markets.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
                AddPriceBoostFilterModel.MarketSpecifier = AddPriceBoostFilterModel.Markets.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
                AddPriceBoostFilterModel.MarketName = string.IsNullOrEmpty(AddPriceBoostFilterModel.MarketTemplateId) ? "" : AddPriceBoostFilterModel.Markets.Where(s => s.Value == $"{AddPriceBoostFilterModel.MarketTemplateId}|{AddPriceBoostFilterModel.MarketSpecifier}").FirstOrDefault().Name;
                AddPriceBoostFilterModel.MarketName = GetMarketName(AddPriceBoostFilterModel.MarketName, AddPriceBoostFilterModel.MarketSpecifier);
            }

            AddPriceBoostFilterModel.Selections = await GetSelections(AddPriceBoostFilterModel.ProviderFixtureId, AddPriceBoostFilterModel.MarketTemplateId, AddPriceBoostFilterModel.MarketSpecifier);
            if (AddPriceBoostFilterModel.Selections.Count > 0)
            {
                AddPriceBoostFilterModel.SelectionTemplateId = AddPriceBoostFilterModel.Selections.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
                AddPriceBoostFilterModel.SelectionSpecifier = AddPriceBoostFilterModel.Selections.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
                AddPriceBoostFilterModel.SelectionName = string.IsNullOrEmpty(AddPriceBoostFilterModel.SelectionTemplateId) ? "" : AddPriceBoostFilterModel.Selections.Where(s => s.Value == $"{AddPriceBoostFilterModel.SelectionTemplateId}|{AddPriceBoostFilterModel.SelectionSpecifier}").FirstOrDefault().Name;
            }

            IsPreloader = false;
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnSegmentSelectChanged(ChangeEventArgs e)
        {
            IsPreloader = true;
            AddPriceBoostFilterModel.SegmentId = int.Parse(e.Value.ToString());
            AddPriceBoostFilterModel.SegmentName = AddPriceBoostFilterModel.Segments.Where(s => s.Value == AddPriceBoostFilterModel.SegmentId).FirstOrDefault().Name;

            AddPriceBoostFilterModel.Leagues = new List<SelectListItemModel<int>>();

            AddPriceBoostFilterModel.Leagues = await GetLeagues(AddPriceBoostFilterModel.SportId, AddPriceBoostFilterModel.SegmentId);
            AddPriceBoostFilterModel.LeagueId = AddPriceBoostFilterModel.Leagues.FirstOrDefault()?.Value ?? 0;
            AddPriceBoostFilterModel.LeagueName = AddPriceBoostFilterModel.Leagues.Where(s => s.Value == AddPriceBoostFilterModel.LeagueId).FirstOrDefault().Name;

            AddPriceBoostFilterModel.Fixtures = await GetFixtures(AddPriceBoostFilterModel.SportId, AddPriceBoostFilterModel.SegmentId, AddPriceBoostFilterModel.LeagueId);
            AddPriceBoostFilterModel.ProviderFixtureId = AddPriceBoostFilterModel.Fixtures.FirstOrDefault()?.Value ?? string.Empty;
            AddPriceBoostFilterModel.FixtureName = string.IsNullOrEmpty(AddPriceBoostFilterModel.ProviderFixtureId) ? "" : AddPriceBoostFilterModel.Fixtures.Where(s => s.Value == AddPriceBoostFilterModel.ProviderFixtureId).FirstOrDefault().Name;

            AddPriceBoostFilterModel.Markets = await GetMarkets(AddPriceBoostFilterModel.ProviderFixtureId);
            AddPriceBoostFilterModel.MarketTemplateId = AddPriceBoostFilterModel.Markets.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
            AddPriceBoostFilterModel.MarketSpecifier = AddPriceBoostFilterModel.Markets.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
            AddPriceBoostFilterModel.MarketName = string.IsNullOrEmpty(AddPriceBoostFilterModel.MarketTemplateId) ? "" : AddPriceBoostFilterModel.Markets.Where(s => s.Value == $"{AddPriceBoostFilterModel.MarketTemplateId}|{AddPriceBoostFilterModel.MarketSpecifier}").FirstOrDefault().Name;
            AddPriceBoostFilterModel.MarketName = GetMarketName(AddPriceBoostFilterModel.MarketName, AddPriceBoostFilterModel.MarketSpecifier);

            AddPriceBoostFilterModel.Selections = await GetSelections(AddPriceBoostFilterModel.ProviderFixtureId, AddPriceBoostFilterModel.MarketTemplateId, AddPriceBoostFilterModel.MarketSpecifier);
            AddPriceBoostFilterModel.SelectionTemplateId = AddPriceBoostFilterModel.Selections.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
            AddPriceBoostFilterModel.SelectionSpecifier = AddPriceBoostFilterModel.Selections.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
            AddPriceBoostFilterModel.SelectionName = string.IsNullOrEmpty(AddPriceBoostFilterModel.SelectionTemplateId) ? "" : AddPriceBoostFilterModel.Selections.Where(s => s.Value == $"{AddPriceBoostFilterModel.SelectionTemplateId}|{AddPriceBoostFilterModel.SelectionSpecifier}").FirstOrDefault().Name;

            IsPreloader = false;
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnLeagueSelectChanged(ChangeEventArgs e)
        {
            IsPreloader = true;
            AddPriceBoostFilterModel.LeagueId = int.Parse(e.Value.ToString());
            AddPriceBoostFilterModel.LeagueName = AddPriceBoostFilterModel.Leagues.Where(s => s.Value == AddPriceBoostFilterModel.LeagueId).FirstOrDefault().Name;

            AddPriceBoostFilterModel.Fixtures = await GetFixtures(AddPriceBoostFilterModel.SportId, AddPriceBoostFilterModel.SegmentId, AddPriceBoostFilterModel.LeagueId);
            AddPriceBoostFilterModel.ProviderFixtureId = AddPriceBoostFilterModel.Fixtures.FirstOrDefault()?.Value ?? string.Empty;
            AddPriceBoostFilterModel.FixtureName = string.IsNullOrEmpty(AddPriceBoostFilterModel.ProviderFixtureId) ? "" : AddPriceBoostFilterModel.Fixtures.Where(s => s.Value == AddPriceBoostFilterModel.ProviderFixtureId).FirstOrDefault().Name;

            AddPriceBoostFilterModel.Markets = await GetMarkets(AddPriceBoostFilterModel.ProviderFixtureId);
            AddPriceBoostFilterModel.MarketTemplateId = AddPriceBoostFilterModel.Markets.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
            AddPriceBoostFilterModel.MarketSpecifier = AddPriceBoostFilterModel.Markets.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
            AddPriceBoostFilterModel.MarketName = string.IsNullOrEmpty(AddPriceBoostFilterModel.MarketTemplateId) ? "" : AddPriceBoostFilterModel.Markets.Where(s => s.Value == $"{AddPriceBoostFilterModel.MarketTemplateId}|{AddPriceBoostFilterModel.MarketSpecifier}").FirstOrDefault().Name;
            AddPriceBoostFilterModel.MarketName = GetMarketName(AddPriceBoostFilterModel.MarketName, AddPriceBoostFilterModel.MarketSpecifier);

            AddPriceBoostFilterModel.Selections = await GetSelections(AddPriceBoostFilterModel.ProviderFixtureId, AddPriceBoostFilterModel.MarketTemplateId, AddPriceBoostFilterModel.MarketSpecifier);
            AddPriceBoostFilterModel.SelectionTemplateId = AddPriceBoostFilterModel.Selections.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
            AddPriceBoostFilterModel.SelectionSpecifier = AddPriceBoostFilterModel.Selections.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
            AddPriceBoostFilterModel.SelectionName = string.IsNullOrEmpty(AddPriceBoostFilterModel.SelectionTemplateId) ? "" : AddPriceBoostFilterModel.Selections.Where(s => s.Value == $"{AddPriceBoostFilterModel.SelectionTemplateId}|{AddPriceBoostFilterModel.SelectionSpecifier}").FirstOrDefault().Name;

            IsPreloader = false;
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnFixtureSelectChanged(ChangeEventArgs e)
        {
            IsPreloader = true;
            AddPriceBoostFilterModel.ProviderFixtureId = e.Value.ToString();
            AddPriceBoostFilterModel.FixtureName = string.IsNullOrEmpty(AddPriceBoostFilterModel.ProviderFixtureId) ? "" : AddPriceBoostFilterModel.Fixtures.Where(s => s.Value == AddPriceBoostFilterModel.ProviderFixtureId).FirstOrDefault().Name;

            AddPriceBoostFilterModel.Markets = await GetMarkets(AddPriceBoostFilterModel.ProviderFixtureId);
            AddPriceBoostFilterModel.MarketTemplateId = AddPriceBoostFilterModel.Markets.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
            AddPriceBoostFilterModel.MarketSpecifier = AddPriceBoostFilterModel.Markets.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
            AddPriceBoostFilterModel.MarketName = string.IsNullOrEmpty(AddPriceBoostFilterModel.MarketTemplateId) ? "" : AddPriceBoostFilterModel.Markets.Where(s => s.Value == $"{AddPriceBoostFilterModel.MarketTemplateId}|{AddPriceBoostFilterModel.MarketSpecifier}").FirstOrDefault().Name;
            AddPriceBoostFilterModel.MarketName = GetMarketName(AddPriceBoostFilterModel.MarketName, AddPriceBoostFilterModel.MarketSpecifier);

            AddPriceBoostFilterModel.Selections = await GetSelections(AddPriceBoostFilterModel.ProviderFixtureId, AddPriceBoostFilterModel.MarketTemplateId, AddPriceBoostFilterModel.MarketSpecifier);
            AddPriceBoostFilterModel.SelectionTemplateId = AddPriceBoostFilterModel.Selections.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
            AddPriceBoostFilterModel.SelectionSpecifier = AddPriceBoostFilterModel.Selections.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
            AddPriceBoostFilterModel.SelectionName = string.IsNullOrEmpty(AddPriceBoostFilterModel.SelectionTemplateId) ? "" : AddPriceBoostFilterModel.Selections.Where(s => s.Value == $"{AddPriceBoostFilterModel.SelectionTemplateId}|{AddPriceBoostFilterModel.SelectionSpecifier}").FirstOrDefault().Name;

            IsPreloader = false;
            await InvokeAsync(StateHasChanged);
        }      

        public async Task OnMarketSelectChanged(ChangeEventArgs e)
        {
            IsPreloader = true;

            AddPriceBoostFilterModel.MarketTemplateId = e.Value.ToString().Split('|')[0] ?? string.Empty;
            AddPriceBoostFilterModel.MarketSpecifier = e.Value.ToString().Split('|')[1] ?? string.Empty;
            AddPriceBoostFilterModel.MarketName = string.IsNullOrEmpty(AddPriceBoostFilterModel.MarketTemplateId) ? "" : AddPriceBoostFilterModel.Markets.Where(s => s.Value == $"{AddPriceBoostFilterModel.MarketTemplateId}|{AddPriceBoostFilterModel.MarketSpecifier}").FirstOrDefault().Name;
            AddPriceBoostFilterModel.MarketName = GetMarketName(AddPriceBoostFilterModel.MarketName, AddPriceBoostFilterModel.MarketSpecifier);

            AddPriceBoostFilterModel.Selections = await GetSelections(AddPriceBoostFilterModel.ProviderFixtureId, AddPriceBoostFilterModel.MarketTemplateId, AddPriceBoostFilterModel.MarketSpecifier);
            AddPriceBoostFilterModel.SelectionTemplateId = AddPriceBoostFilterModel.Selections.FirstOrDefault()?.Value.Split('|')[0] ?? string.Empty;
            AddPriceBoostFilterModel.SelectionSpecifier = AddPriceBoostFilterModel.Selections.FirstOrDefault()?.Value.Split('|')[1] ?? string.Empty;
            AddPriceBoostFilterModel.SelectionName = string.IsNullOrEmpty(AddPriceBoostFilterModel.SelectionTemplateId) ? "" : AddPriceBoostFilterModel.Selections.Where(s => s.Value == $"{AddPriceBoostFilterModel.SelectionTemplateId}|{AddPriceBoostFilterModel.SelectionSpecifier}").FirstOrDefault().Name;

            IsPreloader = false;
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnSelectionSelectChanged(ChangeEventArgs e)
        {
            IsPreloader = true;

            if (!string.IsNullOrEmpty(e.Value.ToString()))
            {
                AddPriceBoostFilterModel.SelectionTemplateId = e.Value.ToString();
                AddPriceBoostFilterModel.SelectionTemplateId = e.Value.ToString().Split('|')[0] ?? string.Empty;
                AddPriceBoostFilterModel.SelectionSpecifier = e.Value.ToString().Split('|')[1] ?? string.Empty;
                AddPriceBoostFilterModel.SelectionName = string.IsNullOrEmpty(AddPriceBoostFilterModel.SelectionTemplateId) ? "" : AddPriceBoostFilterModel.Selections.Where(s => s.Value == $"{AddPriceBoostFilterModel.SelectionTemplateId}|{AddPriceBoostFilterModel.SelectionSpecifier}").FirstOrDefault().Name;
            }

            IsPreloader = false;
            await InvokeAsync(StateHasChanged);
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
                marketFixtureresponse = (await TradingService.GetSportsBookFixtures(new MarketFixtureRequest
                {
                    FixtureTypeId = (int)SelectedFixtureType,
                    LeagueId = leagueId,
                    SegmentId = segmentId,
                    SportId = sportId,
                })).Result;

            }
            else
            {
                marketFixtureresponse = (await TradingService.GetSportsBookOutrightFixtures(new MarketFixtureRequest
                {
                    FixtureTypeId = (int)SelectedFixtureType,
                    LeagueId = leagueId,
                    SegmentId = segmentId,
                    SportId = sportId,
                })).Result;

            }

            if (marketFixtureresponse == null)
                return new List<SelectListItemModel<string>>();

            if (marketFixtureresponse.Fixtures.Count > 0)
                marketFixtureresponse.Fixtures = marketFixtureresponse.Fixtures.Where(f => f.FixtureStatusId < 1).ToList();

            if (marketFixtureresponse.Fixtures.Count == 0)
                return new List<SelectListItemModel<string>>();

            var fixtures = marketFixtureresponse.Fixtures.Select(s => new SelectListItemModel<string>()
            {
                Value = s.ProviderFixtureId,
                Name = s.FixtureName
            }).Distinct().ToList();

            return fixtures.OrderBy(o => o.Name).ToList();
        }

        private async Task<List<SelectListItemModel<string>>> GetMarkets(string providerFixtureId)
        {
            if (marketFixtureresponse == null)
                return new List<SelectListItemModel<string>>();

            var fixture = marketFixtureresponse.Fixtures.Where(f => f.ProviderFixtureId == providerFixtureId).FirstOrDefault();
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
            if (marketFixtureresponse == null)
                return new List<SelectListItemModel<string>>();

            var fixture = marketFixtureresponse.Fixtures.Where(f => f.ProviderFixtureId == providerFixtureId).FirstOrDefault();
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

        private async Task<ClientModel> GetClients()
        {
            ClientModel clients = new ClientModel();
            clients = (await BackOfficeService.GetClients()).Result;

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
