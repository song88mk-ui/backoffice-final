using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.OddsEngine.ManageBoost;
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
        [Parameter] public string Region { get; set; }

        public List<PriceBoostConfigModel> PriceBoostList { get; set; }
        public List<BreadcrumbModel> Breadcrumbs { get; set; }

        public Filter FilterReference { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }                
                
        private bool IsPreLoader { get; set; }
        private decimal Opacity { get; set; } = 0;
        private string AlertMessage { get; set; }
        private AlertStatus AlertStatus { get; set; }

        #endregion

        #region Fields
        public LanguagesModel _languagesModel { get; set; }
        private string _search { get; set; }        
        public string _clientCode { get; set; }
        public int _sportId { get; set; }
        public int _segmentId { get; set; }
        public int _leagueId { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            TradingService.Region = Region.ToUpper();
            PageTitle = "Manage Boost";
            IconClass = string.Empty;
            IsPreLoader = true;
            TradingService.Region = Region.ToUpper();
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
                    Url = $"{Region}/odds-engine/manage-boost",
                    IsCurrentPage = true
                }
            };            
        }

        public async Task OnEditClickEvent(PriceBoostConfigModel priceboost)
        {
            _languagesModel = new LanguagesModel();

            _languagesModel.Languages = _languagesModel.Languages.Where(c => !priceboost.BoostedSelectionTranslations
            .Select(s => s.Language.LanguageCode).ToList().Contains(c.LanguageCode)).ToList();            

            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 90rem;top:50%;transform:translateY(-50%)");            
            parameters.Add("PriceBoostConfigModel", priceboost);
            parameters.Add("PriceBoostConfigModelSerialized", JsonSerializer.Serialize(priceboost));
            parameters.Add("Languages", _languagesModel);
            parameters.Add("Region", Region);

            var options = new ModalOptions();

            ModalService.Open<EditBoostedOddsModal>($"Edit Boosted Odds", parameters, options);
        }

        public async Task OnViewLogClickEvent(PriceBoostConfigModel priceboost)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("PriceBoostConfigModel", priceboost);
            parameters.Add("Region", Region);

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
            parameters.Add("Region", Region);

            var options = new ModalOptions();

            ModalService.Open<DeleteBoostedOddsModal>($"Delete Boosted Odds", parameters, options);
        }

        public async Task OnInitCallback()
        {
            await GetContents();
        }
        
        public async Task OnClientChangedEvent(string clientCode)
        {
            _clientCode = clientCode;

            await GetContents();
        }

        public async Task OnSportChangedEvent(int sportId)
        {
            _sportId = sportId;
            _segmentId = 0;
            _leagueId = 0;

            await GetContents();
        }

        public async Task OnSegmentChangedEvent(int segmentId)
        {
            _segmentId = segmentId;
            _leagueId = 0;

            await GetContents();
        }

        public async Task OnLeagueChangedEvent(int leagueId)
        {
            _leagueId = leagueId;

            await GetContents();
        }

        public async Task OnInputSearchEvent(string search)
        {
            _search = search;

            await GetContents();
        }

        public async Task OnModalSave(object message)
        {
            _clientCode = string.Empty;
            _sportId = 0;
            _segmentId = 0;
            _leagueId = 0;
            _search = string.Empty;

            FilterReference.SelectedClientCode = _clientCode;
            FilterReference.SelectedSportId = _sportId;
            FilterReference.SelectedSegmentId = _segmentId;
            FilterReference.SelectedLeagueId = _leagueId;
            FilterReference.SearchForm.SearchText = _search;

            Opacity = 100;
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
            parameters.Add("Region", Region);

            var options = new ModalOptions();

            ModalService.Open<AddBoostedOddsModal>($"Add Boosted Odds", parameters, options);
        }
        public async Task OpacityChanged(decimal opacity)
        {
            Opacity = opacity;

            if (opacity == 0)
                AlertMessage = string.Empty;
        }

        public void Dispose()
        {
            PriceBoostList = null;
            Breadcrumbs = null;
            FilterReference = null;
            _languagesModel = null;

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        private async Task GetContents()
        {
            IsPreLoader = true;
            StateHasChanged();

            var priceBoostConfigResponse = (await TradingService.GetPriceBoostConfig(new PriceBoostConfigRequest
            {
                ClientCode = !string.IsNullOrEmpty(_clientCode) ? _clientCode : string.Empty,
                SportId = _sportId > 0 ? _sportId.ToString() : string.Empty,
                SegmentId = _segmentId > 0 ? _segmentId.ToString() : string.Empty,
                LeagueId = _leagueId > 0 ? _leagueId.ToString() : string.Empty,
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

                    if (!string.IsNullOrEmpty(_search))
                    {
                        var search = _search.ToLower();

                        query = query.Where(c => c.ClientName.ToLower().Contains(_search) || c.SportName.ToLower().Contains(_search)
                        || c.SegmentName.ToLower().Contains(_search) || c.LeagueName.ToLower().Contains(_search)
                        || c.SelectionName.ToLower().Contains(_search));
                    }

                    PriceBoostList = query.ToList();
                }
            }

            IsPreLoader = false;
        }

        #endregion
    }
}
