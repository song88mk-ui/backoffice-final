using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.ManageBoost;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ManageBoost : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        public Filter FilterReference { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        public MetaData MetaData { get; set; }
        public List<PriceBoostConfigModel> PriceBoostList { get; set; }
        public LanguagesModel LanguagesModel { get; set; }
        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public string Search { get; set; }

        public bool IsPreLoader { get; set; }
        public string AlertMessage { get; set; }
        public AlertStatus AlertStatus { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Manage Boost";
            IconClass = string.Empty;

            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Price Boost",
                    Url = string.Empty,
                    IsCurrentPage = false
                },
                new BreadcrumbModel
                {
                Name = "Manage Boost",
                    Url = "odds-engine/manage-boost",
                    IsCurrentPage = true
                }
            };

            MetaData = new MetaData();
        }

        public async Task OnEditClickEvent(PriceBoostConfigModel priceboost)
        {
            LanguagesModel = new LanguagesModel();

            LanguagesModel.Languages = LanguagesModel.Languages.Where(c => !priceboost.BoostedSelectionTranslations
            .Select(s => s.Language.LanguageCode).ToList().Contains(c.LanguageCode)).ToList();

            //string obj = JsonSerializer.Serialize(priceboost);

            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 90rem;top:50%;transform:translateY(-50%)");
            //parameters.Add("PriceBoostConfigModel", JsonSerializer.Deserialize<PriceBoostConfigModel>(obj));
            parameters.Add("PriceBoostConfigModel", priceboost);
            parameters.Add("PriceBoostConfigModelSerialized", JsonSerializer.Serialize(priceboost));
            parameters.Add("Languages", LanguagesModel);            

            var options = new ModalOptions();

            ModalService.Open<EditBoostedOddsModal>($"Edit Boosted Odds", parameters, options);
        }

        public async Task OnViewLogClickEvent(PriceBoostConfigModel priceboost)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("PriceBoostConfigModel", priceboost);

            var options = new ModalOptions();

            ModalService.Open<ViewLogsBoostedModal>($"View Logs", parameters, options);
        }

        public async Task OnDeleteClickEvent(PriceBoostConfigModel priceboost)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 80rem;top:50%;transform:translateY(-50%)");
            parameters.Add("PriceBoostConfigModel", priceboost);
            parameters.Add("PriceBoostConfigModelSerialized", JsonSerializer.Serialize(priceboost));

            var options = new ModalOptions();

            ModalService.Open<DeleteBoostedOddsModal>($"Delete Boosted Odds", parameters, options);
        }

        public async Task OnInitCallback()
        {
            await GetContents();
        }
        
        public async Task OnClientChangedEvent(string clientCode)
        {
            ClientCode = clientCode;

            await GetContents();
        }

        public async Task OnSportChangedEvent(int sportId)
        {
            SportId = sportId;
            SegmentId = 0;
            LeagueId = 0;

            //PriceBoostFilterModel.SegmentId = 0;
            //PriceBoostFilterModel.LeagueId = 0;

            //PriceBoostFilterModel.Segments = await LoadSegments(sportId);
            //PriceBoostFilterModel.Leagues = await LoadLeagues(sportId, PriceBoostFilterModel.SegmentId);

            await GetContents();
        }

        public async Task OnSegmentChangedEvent(int segmentId)
        {
            SegmentId = segmentId;
            LeagueId = 0;
            //PriceBoostFilterModel.LeagueId = 0;

            //PriceBoostFilterModel.Leagues = await LoadLeagues(param.Item1, param.Item2);

            await GetContents();
        }

        public async Task OnLeagueChangedEvent(int leagueId)
        {
            LeagueId = leagueId;

            await GetContents();
        }

        public async Task OnInputSearchEvent(string search)
        {
            Search = search;

            await GetContents();
        }

        public async Task OnModalSave(object message)
        {
            ClientCode = string.Empty;
            SportId = 0;
            SegmentId = 0;
            LeagueId = 0;
            Search = string.Empty;

            FilterReference.SelectedClientCode = ClientCode;
            FilterReference.SelectedSportId = SportId;
            FilterReference.SelectedSegmentId = SegmentId;
            FilterReference.SelectedLeagueId = LeagueId;
            FilterReference.SearchForm.SearchText = Search;

            AlertMessage = $"Price Boost has been {(string)message}!";
            AlertStatus = AlertStatus.Success;

            await GetContents();
        }

        public async Task OnModalBack(object addPriceBoostFilterModel)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("AddPriceBoostFilterModel", (AddPriceBoostFilterModel)addPriceBoostFilterModel);

            var options = new ModalOptions();

            ModalService.Open<AddBoostedOddsModal>($"Add Boosted Odds", parameters, options);
        }

        public void Dispose()
        {
            TradingService.Dispose();
        }

        #endregion

        #region Utilities

        private async Task GetContents()
        {
            IsPreLoader = true;

            var priceBoostConfigResponse = (await TradingService.GetPriceBoostConfig(new PriceBoostConfigRequest
            {
                ClientCode = !string.IsNullOrEmpty(ClientCode) ? ClientCode : string.Empty,
                SportId = SportId > 0 ? SportId.ToString() : string.Empty,
                SegmentId = SegmentId > 0 ? SegmentId.ToString() : string.Empty,
                LeagueId = LeagueId > 0 ? LeagueId.ToString() : string.Empty,
                ProviderFixtureId = string.Empty,
                MarketTemplateId = string.Empty,
                SelectionTemplateId = string.Empty,
                SelectionSpecifier = string.Empty,
                MarketSpecifier = string.Empty
            }));

            if (priceBoostConfigResponse.StatusCode == StatusCode.Success)
            {
                if (priceBoostConfigResponse.Result == null || !priceBoostConfigResponse.Result.Any())
                {
                    PriceBoostList = new List<PriceBoostConfigModel>();
                }
                else
                {
                    var query = priceBoostConfigResponse.Result.AsQueryable();

                    query = query.Where(c => UserModel.ClientCodes.Contains(c.ClientCode));

                    if (!string.IsNullOrEmpty(Search))
                    {
                        var search = Search.ToLower();

                        query = query.Where(c => c.ClientName.ToLower().Contains(search) || c.SportName.ToLower().Contains(search)
                        || c.SegmentName.ToLower().Contains(search) || c.LeagueName.ToLower().Contains(search)
                        || c.SelectionName.ToLower().Contains(search));
                    }

                    PriceBoostList = query.ToList();
                }
            }

            IsPreLoader = false;
        }

        #endregion
    }
}
